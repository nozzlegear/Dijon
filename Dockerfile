FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS Builder
WORKDIR /app

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
RUN dotnet publish -c Release -o dist src/Dijon.Bot/Dijon.Bot.fsproj

# Switch to alpine for running the application
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled-extra AS Runlayer
WORKDIR /app

# Fix SqlClient invariant errors when dotnet core runs in an alpine container
# https://github.com/dotnet/SqlClient/issues/220
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the built files from Builder container
COPY --from=0 /app/dist /app/dist
COPY --from=0 /app/testresults /app/testresults

# Run the built executable on startup
ENTRYPOINT ["dotnet", "/app/dist/Dijon.Bot.dll"]
