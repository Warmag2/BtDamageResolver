FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
WORKDIR "/build/src/Silo"
RUN dotnet publish "Silo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 11111
EXPOSE 30000
ENTRYPOINT ["dotnet", "Silo.dll"]
