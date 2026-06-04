FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
COPY ["tools/", "tools/"]
WORKDIR "/build/tools/DataExporter"
RUN dotnet publish "DataExporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
COPY --chown=app:app exportdata.sh .
RUN mkdir -p data && chown app:app data
USER app
CMD ["sh", "exportdata.sh"]
