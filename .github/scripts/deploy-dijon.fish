#! /usr/bin/env fish

set LOG_FILE "/var/log/deploy-dijon.log"

function printErr -a msg
    set_color red
    echo "$msg" >&2
    set_color normal
end

function log -a msg
    set timestamp (date -u "+%F T%TZ")

    # Echo to the log file and to the console
    echo "[$timestamp]: $msg" >> "$LOG_FILE"
    echo "$msg"
end

function isArm64
    # Note that functions in fish return exit codes, not boolean true/false
    if test (uname -m) = "arm64"
        true
    else
        false
    end
end

# A function to format a list of secrets into `podman run` args
function formatSecrets
    for secret in $argv
        # Replace the "Dijon_" prefix in the secret name, so the secret gets created at /run/secret/MY_SECRET instead of /run/secret/DIJON_MY_SECRET
        set withoutPrefix (string replace "Dijon_" "" "$secret")
        echo "--secret=$secret,target=$withoutPrefix"
    end
end

# Formats the database container's system user. If the host is arm64, this will set the user to root, which is required for the Azure SQL Edge db image.
function formatDbSystemUser
    if isArm64
        echo "-u=root"
    end
end

set POD_NAME "dijon_pod"

set BOT_IMAGE "$argv[1]"
set BOT_CONTAINER_NAME "dijon_bot"

set DB_IMAGE "mcr.microsoft.com/mssql/server:2017-latest-ubuntu"
set DB_CONTAINER_NAME "dijon_db"
set DB_HOST_PORT_MAP "4001:1433"

# Also check if the volume location is overridden. Again, this is to test the bot on MacOS where volumes must be in a specific location set up during `podman machine init`.
if set -q DIJON_DB_VOLUME_LOCATION
    log "Using \$DIJON_DB_VOLUME_LOCATION value $DIJON_DB_VOLUME_LOCATION"
    set DB_VOLUME_LOCATION "$DIJON_DB_VOLUME_LOCATION"
else
    set DB_VOLUME_LOCATION "/var/www/dijon/volume/db/"
end

# If the user is on arm, change the db image to Azure SQL Edge
if isArm64
    set -l PREVIOUS_IMAGE "$DB_IMAGE"
    set DB_IMAGE "mcr.microsoft.com/azure-sql-edge:1.0.5"

    set_color yellow
    log "Detected arm64 host! $DB_IMAGE is not available for arm64, swapping to $DB_IMAGE which supports arm64."
    set_color normal
end

# Secrets needed by the bot
set BOT_SECRETS_LIST "Dijon_Twitch__ClientSecret" \
    "Dijon_Discord__ApiToken" \
    "Dijon_Database__ConnectionStrings__DefaultConnection"

# Secrets needed by the database
set DB_SECRETS_LIST "Dijon_Database__SqlDatabase__Password"

if test -z "$BOT_IMAGE"
    printErr "No image given, cannot deploy update."
    set_color yellow
    echo "Usage: ./script.fish example.azurecr.io/image:version"
    exit 1
end

if test ! -d "$DB_VOLUME_LOCATION"
    mkdir -p "$DB_VOLUME_LOCATION"
    or exit 1
end

if ! command -q podman
    printErr "`podman` command not found. Is podman installed? Does `podman ps` work? Does `command -v podman` work?"
    exit 1
end

# Check that all secrets are set
for secret in $BOT_SECRETS_LIST $DB_SECRETS_LIST
    if ! podman secret inspect "$secret" > /dev/null
        printErr "podman secret \"$secret\" is missing. You must manually set up secrets on the host before deploying."
        exit 1
    end
end

# Update the images
log "Pulling bot image from $BOT_IMAGE..."
podman pull "$BOT_IMAGE"
or exit 1

# Remove the existing container so it can be updated
if podman container exists "$BOT_CONTAINER_NAME"
    log "Removing container $BOT_CONTAINER_NAME..."
    podman stop "$BOT_CONTAINER_NAME"
    and podman rm "$BOT_CONTAINER_NAME"
end

# If the pod exists, stop it for updates.
if podman pod exists "$POD_NAME"
    log "Stopping existing pod $POD_NAME..."
    podman pod stop "$POD_NAME" --time 5
    or exit 1
else
    # Create the pod. When using pods, it's the pod that must publish ports, not the container.
    log "Creating pod $POD_NAME..."
    podman pod create \
        --name "$POD_NAME" \
        --publish "$DB_HOST_PORT_MAP"
    or exit 1
end

# Create the bot container, but don't start it.
log "Creating container $BOT_CONTAINER_NAME..."
podman create \
    --restart "unless-stopped" \
    --name "$BOT_CONTAINER_NAME" \
    --pod "$POD_NAME" \
    -it \
    (formatSecrets $BOT_SECRETS_LIST) \
    "$BOT_IMAGE"
or exit 1

# The database image won't be updated or changed with every deployment. Only create it if it doesn't already exist.
if ! podman container exists "$DB_CONTAINER_NAME"
    log "Pulling db image from $DB_IMAGE..."
    podman pull "$DB_IMAGE"
    or exit 1

    log "Creating container $DB_CONTAINER_NAME..."
    podman create \
        --restart "unless-stopped" \
        --name "$DB_CONTAINER_NAME" \
        --env "ACCEPT_EULA=Y" \
        --pod "$POD_NAME" \
        --volume "$DB_VOLUME_LOCATION:/var/opt/mssql" \
        -it \
        (formatDbSystemUser) \
        (formatSecrets $DB_SECRETS_LIST) \
        "$DB_IMAGE"
    or exit 1
end

# Start the pod and containers within
log "Starting pod $POD_NAME..."
podman pod start "$POD_NAME"
or exit 1

log "Done!"
