#! /usr/bin/env fish

# Load dev environment variables from env.fish file
. env.fish

set -x ASPNETCORE_ENVIRONMENT "development"
set containerName "dijon_db_1"
set containerPort "$DIJON_SQL_DATABASE_PORT"
set sqlPassword "$DIJON_SQL_DATABASE_PASSWORD"
set connectionString "$DIJON_SQL_CONNECTION_STRING"
set useSudoForDocker

# Check if the user can use Docker without root
if docker ps &> /dev/null
    set useSudoForDocker false
else if sudo docker ps &> /dev/null
    set useSudoForDocker true
else
    set_color red
    echo "`docker ps` and `sudo docker ps` commands failed to return a successful exit code. Is Docker configured properly?"
    set_color normal
    exit 1
end

function runDocker
    if test $useSudoForDocker
        sudo docker $argv
    else
        docker $argv
    end
end

# Check if the sql container exists
if test (runDocker ps -a -f "name=$containerName" -q)
    echo "Starting sql database container..."
    runDocker start "$containerName"; or exit 1
else
    echo "Sql database container does not exist, creating it..."
    echo "Using sql password $sqlPassword"
    runDocker run -d -it --name "$containerName" -e "SA_PASSWORD=$sqlPassword" -e "ACCEPT_EULA=Y" -p "$containerPort:1433" -v "$PWD/volumes/mssql:/var/opt/mssql" mcr.microsoft.com/mssql/server:2017-latest-ubuntu; or exit 1
end 

dotnet watch --project src run -c Debug
