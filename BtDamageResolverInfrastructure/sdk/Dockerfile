FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

FROM base AS build
WORKDIR /build
COPY ["BtDamageResolver/src/", "src/"]
COPY ["BtDamageResolverInfrastructure/sdk/", "."]
WORKDIR "/build/src/Api"
RUN dotnet pack "Api.csproj" -c Release -o /app/packages
WORKDIR "/build/src/Common"
RUN dotnet pack "Common.csproj" -c Release -o /app/packages

FROM base AS final
WORKDIR /build
COPY ["BtDamageResolverInfrastructure/sdk/", "."]
COPY --from=build ["/app/packages/*.nupkg", "customnugets/"]
