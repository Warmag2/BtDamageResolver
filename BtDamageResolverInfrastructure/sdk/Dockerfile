FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

FROM base AS build
WORKDIR /build
COPY ["CompressionLzma/src/", "src/"]
WORKDIR "/build/src/CompressionLzma"
RUN dotnet build "CompressionLzma.csproj" -c Release
RUN dotnet pack "CompressionLzma.csproj" -c Release -o /build/customnugets
WORKDIR /build
COPY ["BtDamageResolver/src/", "src/"]
COPY ["BtDamageResolverInfrastructure/sdk/", "."]
WORKDIR "/build/src/Api"
RUN dotnet build "Api.csproj" -c Release
RUN dotnet pack "Api.csproj" -c Release -o /build/customnugets
WORKDIR "/build/src/Common"
RUN dotnet build "Common.csproj" -c Release
RUN dotnet pack "Common.csproj" -c Release -o /build/customnugets

FROM base AS final
WORKDIR /build
COPY ["BtDamageResolverInfrastructure/sdk/", "."]
COPY --from=build ["/build/customnugets/*.nupkg", "customnugets/"]
