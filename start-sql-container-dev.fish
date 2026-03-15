#! /usr/bin/env fish

set volumeLocation "$HOME/.local/volumes/dijon/postgres"
set dbImage "postgres:17-alpine"
set containerName "dijon_db"
set containerPort "5432"
set dbPassword "a-BAD_passw0rd"

if test ! -d "$volumeLocation"
    mkdir -p "$volumeLocation"
end

# Figure out whether to use podman, docker or sudo docker to start containers
if command -q podman
    set USE_PODMAN 1
else
    set USE_PODMAN 0

    # Check if the user can use Docker without sudo
    if docker ps &> /dev/null
        set USE_SUDO_FOR_DOCKER 0
    else if sudo docker ps &> /dev/null
        set USE_SUDO_FOR_DOCKER 1
    else
        echo "'podman', 'docker ps' and 'sudo docker ps' commands failed to return a successful exit code. Are Podman or Docker configured properly? Do 'podman ps', 'docker ps' or 'sudo docker ps' work?"
        exit 1
    end
end

function pod
    if test $USE_PODMAN -eq 1
        podman $argv
    else if test $USE_SUDO_FOR_DOCKER -eq 1
        sudo docker $argv
    else
        docker $argv
    end
end

# Check if the container exists
if test (pod ps -a -f "name=$containerName" -q)
    echo "Starting database container..."
    pod start "$containerName"
    or exit 1
else
    echo "Container $containerName does not exist, creating it..."
    echo "Using password $dbPassword"
    pod run \
        -dit \
        --name "$containerName" \
        -e "POSTGRES_PASSWORD=$dbPassword" \
        -e "POSTGRES_DB=dijon" \
        -p "$containerPort:5432" \
        -v "$volumeLocation:/var/lib/postgresql/data" \
        "$dbImage"
    or exit 1
end
