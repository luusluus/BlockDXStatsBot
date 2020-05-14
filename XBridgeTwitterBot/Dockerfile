FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["XBridgeTwitterBot/XBridgeTwitterBot.csproj", "XBridgeTwitterBot/"]
RUN dotnet restore "XBridgeTwitterBot/XBridgeTwitterBot.csproj"
COPY . .
WORKDIR "/src/XBridgeTwitterBot"
RUN dotnet build "XBridgeTwitterBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XBridgeTwitterBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XBridgeTwitterBot.dll"]