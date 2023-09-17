FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.18 as Builder
WORKDIR /app

# Configure dotnet
ENV DOTNET_USE_POLLING_FILE_WATCHER 1
ENV PATH="${PATH}:/root/.dotnet/tools"

# Restore package dependencies
COPY Directory.Packages.props .
COPY Dijon.sln .
COPY src/Dijon.Bot/Dijon.Bot.fsproj src/Dijon.Bot/
COPY src/tests/Dijon.Tests.fsproj src/tests/
COPY src/Dijon.Migrations/Dijon.Migrations.fsproj src/Dijon.Migrations/
RUN dotnet restore

# Copy source files and build project
COPY src/Dijon.Bot/ src/Dijon.Bot/
COPY src/tests/ src/tests/
COPY src/Dijon.Migrations/ src/Dijon.Migrations/

# Run the tests
RUN dotnet test --results-directory /app/testresults --logger "trx;LogFileName=testresults.xml"

# Publish the project
RUN dotnet publish src/Dijon.Bot/Dijon.Bot.fsproj -c Release -o dist -r linux-musl-x64

# Switch to alpine for running the application
FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine3.18
WORKDIR /app

# Fix SqlClient invariant errors when dotnet core runs in an alpine container
# https://github.com/dotnet/SqlClient/issues/220
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Add timezone info (tzdata package) to Alpine
# https://github.com/dotnet/dotnet-docker/issues/1366
RUN apk add --no-cache tzdata

# Copy the built files from Builder container
COPY --from=0 /app/dist /app/dist
COPY --from=0 /app/testresults /app/testresults
RUN chmod +x /app/dist/Dijon.Bot

# Run the built executable on startup
CMD [ "/app/dist/Dijon" ]
