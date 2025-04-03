FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

FROM base AS build
WORKDIR /build
COPY ["BtDamageResolver/src/", "src/"]
COPY ["BtDamageResolverInfrastructure/sdk/", "."]
WORKDIR "/build/src/Api"
RUN dotnet publish "Api.csproj" -c Release -o /app/publish
WORKDIR "/build/src/Common"
RUN dotnet publish "Common.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /build
COPY ["BtDamageResolverInfrastructure/sdk/nuget.config", "."]
COPY --from=build /app/publish/*.nupkg .
RUN mkdir customnugets
RUN mv *.nupkg ./customnugets/
