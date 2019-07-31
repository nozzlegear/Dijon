FROM fsharp:netcore as Builder
WORKDIR /app

# Install paket
COPY .paket .paket
RUN mono .paket/paket.bootstrapper.exe

COPY Dijon.sln .
COPY paket.lock .
COPY paket.dependencies .
COPY src/paket.references src/
COPY src/Dijon.fsproj src/

RUN dotnet restore

COPY src/* src/

RUN dotnet publish -c Release -o dist -r linux-musl-x64

# Switch to alpine
FROM microsoft/dotnet:2.2-runtime-alpine
WORKDIR /app

# Copy the built files from fsharp
COPY --from=0 /app/src/dist ./dist
RUN chmod +x ./dist/Dijon 

# ENTRYPOINT ["src/dist/Dijon"]
CMD [ "/app/dist/Dijon" ]
