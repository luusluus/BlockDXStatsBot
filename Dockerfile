FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /source

COPY ./src/XBridgeTwitterBot.sln .
COPY ./src/XBridgeTwitterBot/XBridgeTwitterBot.csproj ./XBridgeTwitterBot/

RUN dotnet restore

COPY ./src/XBridgeTwitterBot ./XBridgeTwitterBot

RUN dotnet publish -c Release -o ../dist

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS runtime
WORKDIR /app
COPY --from=build /dist . 
EXPOSE 80 443
ENTRYPOINT ["dotnet", "XBridgeTwitterBot.dll"]