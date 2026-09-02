# https://just.systems
set shell := ["pwsh", "-c"]
set script-interpreter := ["pwsh", "-c"]

repo := "ghcr.io/nozzlegear/dijon"
controlSocket := "/tmp/ssh-control-dijon"
ssh_opts := "-o StrictHostKeyChecking=yes -o SendEnv=no -o ControlMaster=auto -o ControlPath=" + controlSocket + " -o ControlPersist=30s"
quadletTmpDir := "/tmp/dijon-quadlet"

[private]
default:
    @just --list

# =============================================================================
# Quadlet Generation
# =============================================================================

# Generates the quadlet files needed for deployment
[group("release")]
[group("quadlet")]
pkl env="prod" image="ghcr.io/nozzlegear/dijon:latest" output_dir="quadlet/output":
    mkdir -p quadlet/output
    pkl eval quadlet/files.pkl -p 'appImageName={{image}}' -m "{{output_dir}}"

# =============================================================================
# Container Build
# =============================================================================

[group("release")]
build tag="latest" commit="":
    $commit = "{{ if commit != '' { commit } else {`git rev-parse head`} }}"
    podman build \
        -t "{{repo}}:{{tag}}" \
        -t "{{repo}}:latest" \
        --build-arg "RUN={{tag}}" \
        --build-arg "COMMIT=$commit" \
        .

[script]
[group("release")]
get-digest tag="latest":
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        skopeo inspect --raw "docker://{{repo}}:{{tag}}" | Set-Content -NoNewLine $tmp
        skopeo manifest-digest $tmp
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }

# =============================================================================
# Deployment
# =============================================================================

[script]
[group("release")]
deploy-quadlets sshTarget quadletDir:
    $sshTarget = "{{sshTarget}}"
    $quadletDir = "{{quadletDir}}"

    try {
        rsync -e "ssh {{ssh_opts}}" "${quadletDir}/" "${sshTarget}:.config/containers/systemd/"
        Write-Output 'Done.'
        $exitCode = $LASTEXITCODE
    } finally {
        just _cleanup-ssh
    }
    if ($exitCode -ne 0) { exit $exitCode }

[script]
[group("release")]
deploy-secrets sshTarget secretFile:
    $secretFile = "{{secretFile}}"
    $sshTarget = "{{sshTarget}}"

    try {
        # Copy decrypted env.production.json to host
        rsync -e "ssh {{ssh_opts}}" "$secretFile" "${sshTarget}:/tmp/appsettings.secrets.json"

        # Create the full secrets file as a podman secret for the app container
        ssh {{ssh_opts}} $sshTarget 'podman secret rm dijon_secrets 2>/dev/null || true'
        ssh {{ssh_opts}} $sshTarget 'podman secret create dijon_secrets /tmp/appsettings.secrets.json'

        # Create individual podman secrets for PostgreSQL from the Postgres section
        ssh {{ssh_opts}} $sshTarget 'podman secret rm dijon_pg_username 2>/dev/null || true'
        ssh {{ssh_opts}} $sshTarget 'set PG_USER (jq -r .Postgres.Username /tmp/appsettings.secrets.json); printf "%s" "$PG_USER" | podman secret create dijon_pg_username -'

        ssh {{ssh_opts}} $sshTarget 'podman secret rm dijon_pg_password 2>/dev/null || true'
        ssh {{ssh_opts}} $sshTarget 'set PG_PASS (jq -r .Postgres.Password /tmp/appsettings.secrets.json); printf "%s" "$PG_PASS" | podman secret create dijon_pg_password -'

        # Clean up
        ssh {{ssh_opts}} $sshTarget 'rm /tmp/appsettings.secrets.json'
        $exitCode = $LASTEXITCODE
    } finally {
        just _cleanup-ssh
    }
    if ($exitCode -ne 0) { exit $exitCode }

[script]
[group("release")]
restart-systemd sshTarget:
    $sshTarget = "{{sshTarget}}"

    try {
        ssh {{ssh_opts}} $sshTarget `
            'systemctl --user daemon-reload && systemctl --user restart dijon-bot.service dijon-db.service'
        $exitCode = $LASTEXITCODE
    } finally {
        just _cleanup-ssh
    }
    if ($exitCode -ne 0) { exit $exitCode }

# =============================================================================
# SSH Cleanup
# =============================================================================

[script]
[private]
[group("release")]
_cleanup-ssh:
    ssh -O exit -o "ControlPath={{controlSocket}}" $sshTarget 2>$null
    Remove-Item {{controlSocket}} -ErrorAction SilentlyContinue
