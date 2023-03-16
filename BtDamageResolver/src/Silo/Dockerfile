FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
RUN dotnet restore "src/Silo/Silo.csproj"
COPY . .
WORKDIR "/build/src/Silo"
RUN dotnet build "Silo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Silo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 11111
EXPOSE 30000
ENTRYPOINT ["dotnet", "Silo.dll"]
