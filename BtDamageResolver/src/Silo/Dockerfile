FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
RUN apk add --no-cache krb5-libs
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
WORKDIR "/build/src/Silo"
RUN dotnet publish "Silo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
USER app
EXPOSE 11111
EXPOSE 30000
ENTRYPOINT ["dotnet", "Silo.dll"]
