from microsoft/dotnet:2.1-sdk

WORKDIR /app

RUN apt update && apt install mono-devel -y

COPY paket.lock .
COPY paket.dependencies .
COPY dijon.sln .
COPY src/Dijon.csproj .src/
COPY src/paket.references .src/
COPY ./.paket ./.paket

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe restore

COPY . .

RUN dotnet publish -c Release -r linux-x64 ./src

ENTRYPOINT ["dotnet", "/app/src/bin/Release/netcoreapp2.1/linux-x64/publish/Dijon.dll"]
