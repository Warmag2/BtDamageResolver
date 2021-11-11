FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
COPY ["tools/", "tools/"]
RUN dotnet restore "tools/DataImporter/DataImporter.csproj"
COPY . .
WORKDIR "/build/tools/DataImporter"
RUN dotnet build "DataImporter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataImporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["data/", "data/"]
COPY importdata.sh .
CMD ["sh", "importdata.sh"]