# Native appsettings + ConnectionStrings refactor

**Date:** 2026-06-02
**Status:** Approved (design)
**Scope:** Silo, BlazorServer client, DataImporter, DataExporter, Common, Services, infra

## Goal

Replace the custom configuration-loading scheme (`ConfigurationUtilities.GetConfiguration`)
with the native .NET host configuration chain; replace the `CommunicationOptions` wrapper class
with the native `ConnectionStrings` block; move the Postgres connection string into
`ConnectionStrings` as well; remove the now-dead `RESOLVER_ENVIRONMENT` / Orleans port-padding
code; and rename the misleadingly-named `orleansdb` container to `resolverpostgres`.

The custom loader was originally written to guarantee the source order
`config file -> environment-override file -> environment variables`. This is **already** the
default order produced by `Host.CreateDefaultBuilder()` / `Host.CreateApplicationBuilder()`, with
environment variables taking precedence over JSON values. The custom loader is therefore redundant.

## Background / current state

- `ConfigurationUtilities.GetConfiguration(settingsFile)` (`src/Common/ConfigurationUtilities.cs:88-118`)
  builds: base JSON -> `{file}.{RESOLVER_ENVIRONMENT}.json` (mandatory if the env var is set; throws
  otherwise) -> `AddEnvironmentVariables()`.
- **Silo** (`src/Silo/Program.cs`): `Host.CreateDefaultBuilder()` with custom config injected via
  `.ConfigureAppConfiguration((_, c) => c.AddConfiguration(GetConfiguration("SiloSettings.json")))`.
- **Client** (`BtDamageResolverClient/src/BlazorServer/Startup.cs`): `ConfigureServices` is `static`,
  ignores the injected `Configuration`, and calls `GetConfiguration("CommunicationSettings.json")`.
- **Tools** (`tools/DataImporter`, `tools/DataExporter`): each has its own private `GetConfiguration`
  that calls only `AddJsonFile(...)` -- **no** `AddEnvironmentVariables()`.
- `CommunicationOptions` holds a Redis `ConnectionString`. `FaemiyahClusterOptions` holds `Invariant`
  + a Postgres `ConnectionString`.

### Package topology (important)

- The **client** (`BlazorServer.csproj`) consumes `Common`/`Api` via **NuGet** `PackageReference`
  (v0.0.447), NOT project references. The **silo, services, tools** use project references.
- Consequence: editing/deleting types in `Common` source does not break the client build until a new
  package is published (owner's separate NuGet workflow). The client must only stop *referencing*
  removed types in its own code.

### Known issues surfaced during analysis

1. **Tool env-var override is dead today.** The tools never call `AddEnvironmentVariables()`, so the
   `CommunicationOptions__ConnectionString` env var for `resolverdataimporter` (`docker-compose.yml:45`)
   is silently ignored; the literal `password=PASSWORD` placeholder is used. This refactor fixes it.
2. **DataExporter reads the wrong file.** `DataExporter.cs:43` calls `GetConfiguration("DataImporterSettings.json")`
   (the importer's file) for its Redis connection, while `Program.cs:32` reads `DataExporterSettings.json`
   for logging. The exporter's `ClusterOptions`/Postgres block is therefore dead. Going native (each tool
   reads its own `appsettings.json`) fixes the cross-file read; the dead Postgres block is dropped.
3. **Dockerfiles do not reference settings filenames**; renaming settings files needs no Dockerfile changes.

## Design

### 1. Configuration loading

Delete `ConfigurationUtilities.GetConfiguration`.

- **Silo** (`src/Silo/Program.cs`): remove the `.ConfigureAppConfiguration(... AddConfiguration ...)` line
  and the top-level `GetConfiguration` call. Read config from `context.Configuration` inside a
  `.UseOrleans((context, siloBuilder) => ...)` delegate. Default chain:
  `appsettings.json -> appsettings.{DOTNET_ENVIRONMENT}.json -> env vars`.
- **Client** (`Startup.cs`): make `ConfigureServices` an **instance** method binding off the injected
  `Configuration`. Remove the `GetConfiguration("CommunicationSettings.json")` call.
- **Tools**: replace each private `GetConfiguration` with `Host.CreateApplicationBuilder(args).Configuration`
  (projects already reference `Microsoft.Extensions.Hosting`). Native chain, no custom code, and each tool
  reads its own `appsettings.json` (fixes finding #2).

### 2. Remove RESOLVER_ENVIRONMENT and the dead port-padding path

In `ConfigurationUtilities`:
- Delete `GetEnvironmentName`, the `EnvironmentName` constant, and `DefaultDebugEnvironmentName`/`DefaultEnvironmentName`.
- Simplify `GetSiloPortConfigurationFromEnvironment`: drop the `portPad` parameter and the Debug-padding block;
  keep the `ORLEANS_CLIENTPORT`/`ORLEANS_SILOPORT` reads and defaults.
- Remove now-unused `using` directives (`System.IO`, `Microsoft.Extensions.Configuration`).

`GetSiloPortConfigurationFromEnvironment()` is already called with no argument (`Program.cs:114`).
After this, `RESOLVER_ENVIRONMENT` no longer appears anywhere. Remaining in `ConfigurationUtilities`:
`GetSiloPortConfigurationFromEnvironment`, `GetHostIp`, and the JSON-serializer helpers.

### 3. CommunicationOptions -> ConnectionStrings:Redis

- Delete `src/Common/Options/CommunicationOptions.cs` and `Settings.CommunicationOptionsBlockName`.
- JSON: `"CommunicationOptions": { "ConnectionString": "redis:..." }` -> `"ConnectionStrings": { "Redis": "redis:..." }`.
- Consumers switch from `IOptions<CommunicationOptions>` to `IConfiguration.GetConnectionString("Redis")`:
  - `src/Silo/Program.cs:227` `GetRedisEntityRepository` -- resolve `IConfiguration` from `serviceProvider`.
  - `src/Services/CommunicationService.cs:35,44` -- inject `IConfiguration`.
  - `BtDamageResolverClient/src/BlazorServer/Communication/ResolverCommunicator.cs:40,368` -- inject `IConfiguration`.
  - `tools/DataImporter/DataImporter.cs:41`, `tools/DataExporter/DataExporter.cs:44` -- read via `GetConnectionString`.
- Remove the now-unused `services.Configure<CommunicationOptions>(...)` registrations and `using` directives.

### 4. Postgres connection string -> ConnectionStrings:Postgres

- JSON: move the Postgres string from `ClusterOptions:ConnectionString` to `ConnectionStrings:Postgres`.
  `ClusterOptions` retains only `Invariant`.
- **Keep `FaemiyahClusterOptions` unchanged** (still `Invariant` + `ConnectionString`) to avoid changing the
  published `Common` package contract and to leave `Services` (`LoggingService`/`LoggingRepository`) untouched.
  In the Silo DI, hydrate the options object:
  ```csharp
  services.Configure<FaemiyahClusterOptions>(configuration.GetSection(Settings.ClusterOptionsBlockName)); // Invariant
  services.PostConfigure<FaemiyahClusterOptions>(o => o.ConnectionString = configuration.GetConnectionString("Postgres"));
  ```
- Silo clustering/grain storage read `Invariant` from the section and the connection string from
  `GetConnectionString("Postgres")`. `LoggingService`/`LoggingRepository` are unchanged (still receive a
  fully-populated `FaemiyahClusterOptions` via `IOptions<>`).
- Connection-string key name: **`Postgres`**.

### 5. Settings.cs documentation fixes

`ClusterOptionsBlockName` and `LoggingOptionsBlockName` XML docs both wrongly say "RabbitMQ"; correct them.
(`CommunicationOptionsBlockName` is deleted in section 3.)

### 6. Rename orleansdb -> resolverpostgres (infra)

The Postgres container is misnamed (it backs Orleans clustering, grain storage, **and** DB logging).
Rename everywhere:
- `infra/docker-compose.yml`: service key, `container_name`, `hostname`, `image: orleansdb:latest`
  -> `resolverpostgres:latest`, both `depends_on: orleansdb` entries, the volume mount and the volume
  declaration `orleansdb-data` -> `resolverpostgres-data`, and `Host=orleansdb` -> `Host=resolverpostgres`
  in the Postgres connection string env var.
- `infra/grafana/datasources/datasource.yml`: `url: orleansdb:5432` -> `resolverpostgres:5432`. The datasource
  `name: Orleansdb` is left unchanged unless verified safe to rename (Grafana dashboards may reference the
  datasource by name).
- `refresh.sh` and `refresh.bat`: `docker build --tag orleansdb ...` -> `--tag resolverpostgres ...`.
- `Host=orleansdb` -> `Host=resolverpostgres` in the Silo `appsettings.json` default.

**Volume:** renamed to `resolverpostgres-data` (accepted). Next `docker compose up` starts with an empty
Postgres DB; the custom Postgres image (`infra/postgresql/Dockerfile`) recreates the Orleans schema on first
init. Persisted Orleans grain/session state and logs are lost; Redis reference data is unaffected.

### 7. File renames + environment mapping

| Old | New |
|---|---|
| `src/Silo/SiloSettings.json` / `.Debug` / `.Release` | `appsettings.json` / `.Debug` / `.Release` |
| `tools/DataImporter/DataImporterSettings.*` | `appsettings.*` |
| `tools/DataExporter/DataExporterSettings.*` | `appsettings.*` (dead Postgres block dropped) |
| Client `CommunicationSettings.json` | merged into existing `appsettings.json` |
| Client `CommunicationSettings.Release.json` | becomes `appsettings.Release.json` |
| Client `CommunicationSettings.Debug.json` | deleted (client uses `ASPNETCORE_ENVIRONMENT`) |

Environment variable per process:
- **Silo + tools:** `RESOLVER_ENVIRONMENT` -> `DOTNET_ENVIRONMENT`.
- **Client:** already `ASPNETCORE_ENVIRONMENT`; drop the redundant `RESOLVER_ENVIRONMENT`.

`.csproj` copy directives:
- `Silo.csproj`, `tools/*/*.csproj` (console SDK): rename `None Update` entries to `appsettings*.json`.
- `BlazorServer.csproj` (Web SDK): `appsettings.json` auto-copies; remove the `CommunicationSettings.*`
  entries; add an explicit copy for `appsettings.Release.json`.

### 8. docker-compose.yml env vars

- `resolver`: `RESOLVER_ENVIRONMENT` -> `DOTNET_ENVIRONMENT`; `CommunicationOptions__ConnectionString`
  -> `ConnectionStrings__Redis`; `ClusterOptions__ConnectionString` -> `ConnectionStrings__Postgres`
  (with `Host=resolverpostgres`).
- `resolverclient`: remove `RESOLVER_ENVIRONMENT`; `CommunicationOptions__ConnectionString` -> `ConnectionStrings__Redis`.
- `resolverdataimporter`: `RESOLVER_ENVIRONMENT` -> `DOTNET_ENVIRONMENT`; `CommunicationOptions__ConnectionString` -> `ConnectionStrings__Redis`.

### Resulting JSON shapes

Silo `appsettings.json`:
```json
{
  "LoggingOptions": { "LogToConsole": true, "LogToDatabase": true, "LogLevel": "Information", "LogLevelOrleans": "Warning", "LoggingIntervalMilliseconds": 15000, "SendDetailedErrorsToClient": true },
  "ClusterOptions": { "Invariant": "Npgsql" },
  "ConnectionStrings": {
    "Postgres": "User ID=USERNAME;Password=PASSWORD;Host=resolverpostgres;Port=5432;Database=BtDamageResolver;SSL Mode=Disable;",
    "Redis": "redis:6379,password=PASSWORD"
  },
  "CompressionOptions": { "Provider": "Brotli", "Quality": 4 }
}
```

Client `appsettings.json` (merge of existing + `CommunicationSettings.json`):
```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft": "Warning", "Microsoft.Hosting.Lifetime": "Information" } },
  "AllowedHosts": "*",
  "LoggingOptions": { "LogToConsole": true, "LogLevel": "Information", "LogLevelOrleans": "Warning" },
  "ConnectionStrings": { "Redis": "redis:6379,password=PASSWORD" },
  "CompressionOptions": { "Provider": "Brotli", "Quality": 4 }
}
```

Tools `appsettings.json` (both importer and exporter):
```json
{
  "LoggingOptions": { "LogToConsole": true, "LogLevel": "Information", "LogLevelOrleans": "Warning" },
  "ConnectionStrings": { "Redis": "redis:6379,password=PASSWORD" }
}
```

## Testing

- **New test** (`tests/Tests`): build a `ConfigurationBuilder` over a sample `appsettings.json` fixture and
  assert (a) `GetConnectionString("Redis")` resolves the JSON value, and (b) a `ConnectionStrings__Redis`
  environment variable **overrides** the JSON value (locks in the precedence behavior that motivated the
  original loader).
- **Build** Silo, BlazorServer, DataImporter, DataExporter, Common, Services, Tests.
- **Manual gate (owner):** `docker compose up` smoke test for real Redis/Postgres connectivity (note the
  Postgres volume reset).

## Risks

- Missing a compose env-var rename -> Redis/Postgres silently uses the `PASSWORD` placeholder. Mitigated by
  the rename tables (sections 7-8) and the precedence test.
- A missed `orleansdb` reference -> resolver/grafana cannot reach Postgres. Mitigated by the section 6 file list.
- Web SDK not copying `appsettings.Release.json` -> empty override at runtime (harmless); mitigated by the
  explicit copy directive.
- `FaemiyahClusterOptions` connection-string source becomes indirect (hydrated via `PostConfigure`); mitigated
  by an explanatory comment at the registration site.

## Out of scope

- Splitting the single Postgres DB into separate clustering/storage/logging databases.
- Renaming `ConfigurationUtilities` (now purely Orleans-port/IP + JSON helpers).
- Renaming the Grafana datasource display name `Orleansdb` (cosmetic; deferred to avoid dashboard breakage).
