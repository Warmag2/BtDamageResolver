FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM resolversdk:latest AS build
WORKDIR /build
COPY ["src/", "src/"]
RUN dotnet restore "src/BlazorServer/BlazorServer.csproj"
COPY . .
WORKDIR "/build/src/BlazorServer"
RUN dotnet build "BlazorServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8787
ENTRYPOINT ["dotnet", "BlazorServer.dll"]
