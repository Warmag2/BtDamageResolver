FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["BtDamageResolver/src/", "src/"]
COPY ["BtDamageResolver/tools/", "tools/"]
WORKDIR "/build/tools/DataImporter"
RUN dotnet publish "DataImporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
COPY --chown=app:app ["BtDamageResolver/data/", "data/"]
USER app
CMD ["dotnet", "DataImporter.dll", "import", "-s", "*.json", "-f", "./data/"]
