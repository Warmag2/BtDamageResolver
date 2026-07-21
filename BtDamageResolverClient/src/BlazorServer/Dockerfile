FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["BtDamageResolverClient/src/", "src/"]
WORKDIR "/build/src/BlazorServer"
RUN dotnet publish "BlazorServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=app:app --from=build /app/publish .
RUN mkdir -p /app/dpkeys && chown app:app /app/dpkeys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlazorServer.dll"]
