FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["BtDamageResolver/src/", "src/"]
COPY ["BtDamageResolver/tools/", "tools/"]
WORKDIR "/build/tools/DataExporter"
RUN dotnet publish "DataExporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
RUN mkdir -p data && chown app:app data
USER app
CMD ["dotnet", "DataExporter.dll", "export", "-f", "./data/"]
