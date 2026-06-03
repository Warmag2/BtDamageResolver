FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
COPY ["tools/", "tools/"]
WORKDIR "/build/tools/DataImporter"
RUN dotnet publish "DataImporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
COPY --chown=app:app ["data/", "data/"]
COPY --chown=app:app importdata.sh .
USER app
CMD ["sh", "importdata.sh"]
