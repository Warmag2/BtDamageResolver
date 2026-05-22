FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
COPY ["tools/", "tools/"]
WORKDIR "/build/tools/DataImporter"
RUN dotnet publish "DataImporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ["data/", "data/"]
COPY importdata.sh .
USER app
CMD ["sh", "importdata.sh"]
