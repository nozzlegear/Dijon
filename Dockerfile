FROM mcr.microsoft.com/dotnet/sdk:5.0.203-focal as Builder
WORKDIR /app

# Configure dotnet and paket
ENV DOTNET_USE_POLLING_FILE_WATCHER 1
RUN dotnet tool install -g paket
ENV PATH="${PATH}:/root/.dotnet/tools"

# Restore package dependencies
COPY .paket .paket
COPY Dijon.sln .
COPY paket.lock .
COPY paket.dependencies .
COPY src/Dijon.fsproj src/
COPY tests/Dijon.Tests.fsproj tests/
COPY Dijon.Migrations/Dijon.Migrations.fsproj Dijon.Migrations/
RUN dotnet restore

# Copy source files and build project
COPY src/ src/
COPY tests/ tests/
COPY Dijon.Migrations/ Dijon.Migrations/

# Run the tests
RUN dotnet test --results-directory /app/testresults --logger "trx;LogFileName=testresults.xml"

# Publish the project
RUN dotnet publish -c Release -o dist -r linux-musl-x64

# Switch to alpine for running the application
FROM mcr.microsoft.com/dotnet/runtime:5.0.9-alpine3.13
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
RUN chmod +x /app/dist/Dijon 

# Run the built executable on startup
CMD [ "/app/dist/Dijon" ]
