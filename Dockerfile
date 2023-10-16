FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.18 as Builder
WORKDIR /app

# Configure dotnet
ENV DOTNET_USE_POLLING_FILE_WATCHER 1
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy source files and restore dependencies. Skipping Docker layer caches because it's unlikely to be useful here
# since fsproj files change whenever a new F# file is added to a project.
COPY nuget.config ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY Dijon.sln ./
COPY src/ ./src/
COPY tests/ ./tests/
#RUN dotnet nuget locals all --clear
#RUN git clean -xfd
RUN dotnet restore --use-lock-file --locked-mode --configfile nuget.config

# Run the tests
RUN dotnet test --no-restore --results-directory /app/testresults --logger "trx;LogFileName=testresults.xml"

# Publish the project
RUN dotnet publish --no-self-contained src/Dijon.Bot/Dijon.Bot.fsproj -c Release -o dist -r linux-musl-x64

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
CMD [ "/app/dist/Dijon.Bot" ]
