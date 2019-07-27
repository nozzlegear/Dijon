FROM fsharp:netcore
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

RUN dotnet publish -c Release -o dist -r linux-x64

ENTRYPOINT ["src/dist/Dijon"]