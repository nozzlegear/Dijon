#! /usr/bin/env fish

# Load dev environment variables from env.fish file
. env.fish

set containerName "dijon_db_1"
set containerPort "$DIJON_SQL_DATABASE_PORT"
set sqlPassword "$DIJON_SQL_DATABASE_PASSWORD"
set connectionString "$DIJON_SQL_CONNECTION_STRING"
set -x ASPNETCORE_ENVIRONMENT "development"

# Check if the sql container exists
if test (docker ps -a -f "name=$containerName" -q)
    echo "Starting sql database container..."
    docker start "$containerName"; or exit 1
else
    echo "Sql database container does not exist, creating it..."
    echo "Using sql password $sqlPassword"
    docker run -d -it --name "$containerName" -e "SA_PASSWORD=$sqlPassword" -e "ACCEPT_EULA=Y" -p "$containerPort:1433" -v "$PWD/volumes/mssql:/var/opt/mssql" mcr.microsoft.com/mssql/server:2017-latest-ubuntu; or exit 1
end 

dotnet watch --project src run -c Debug
