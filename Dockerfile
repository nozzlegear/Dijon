FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as Builder
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
COPY src/paket.references src/
COPY src/Dijon.fsproj src/
COPY Dijon.Migrations/Dijon.Migrations.fsproj Dijon.Migrations/
RUN dotnet restore

# Copy source files and build project
COPY src/ src/
COPY Dijon.Migrations/ Dijon.Migrations/
RUN dotnet publish -c Release -o dist -r linux-musl-x64

# Switch to alpine for smaller container size
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app

# Copy the built files from Builder container
COPY --from=0 /app/dist /app/dist
RUN chmod +x /app/dist/Dijon 

# Run the built executable on startup
CMD [ "/app/dist/Dijon" ]
