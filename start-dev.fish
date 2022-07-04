#! /usr/bin/env fish

# Load dev environment variables from env.fish file
. env.fish

# Start the database container
./start-sql-container-dev.fish

# Set dev environment and start project
set -x ASPNETCORE_ENVIRONMENT "development"
dotnet watch --project src/Dijon/Dijon.fsproj run -c Debug
