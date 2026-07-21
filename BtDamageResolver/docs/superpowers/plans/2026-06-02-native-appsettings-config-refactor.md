# Native appsettings + ConnectionStrings Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the custom `ConfigurationUtilities.GetConfiguration` scheme with the native .NET host config chain, move Redis and Postgres connection strings into native `ConnectionStrings`, delete `CommunicationOptions`, remove `RESOLVER_ENVIRONMENT`, and rename the `orleansdb` container to `resolverpostgres`.

**Architecture:** The silo uses `Host.CreateDefaultBuilder()` (already default-loads `appsettings.json` → `appsettings.{DOTNET_ENVIRONMENT}.json` → env vars). The client's `Startup.ConfigureServices` becomes an instance method binding off the injected `Configuration`. The tools use `Host.CreateApplicationBuilder(args).Configuration`. Connection strings are read via `IConfiguration.GetConnectionString(...)`. `FaemiyahClusterOptions` is kept intact (published in the `Common` NuGet package) and has its `ConnectionString` hydrated via `PostConfigure`.

**Tech Stack:** .NET 10, Microsoft.Orleans 10.1, Microsoft.Extensions.Hosting/Configuration, NUnit 4 + AwesomeAssertions, Docker Compose.

**Repos touched (commit in the repo that contains each path):**
- **Code repo:** `C:\Work\src\BtDamageResolver\BtDamageResolver` (Silo, Common, Services, tools, Tests) — this is the cwd/git repo.
- **Infra:** `C:\Work\src\BtDamageResolver\infra` + `C:\Work\src\BtDamageResolver\refresh.sh|.bat` — run `git status` to confirm which repo tracks these; commit there.
- **Client repo:** `C:\Work\src\BtDamageResolver\BtDamageResolverClient` (BlazorServer). Builds against `Common`/`Api` **NuGet** packages, so it is independent of the `Common` source edits below.

**Spec:** `docs/superpowers/specs/2026-06-02-native-appsettings-config-refactor-design.md`

---

## Task 1: Config precedence regression test

Locks in the two behaviors the original custom loader existed to guarantee: `GetConnectionString("Redis")` resolves the JSON value, and a `ConnectionStrings__Redis` environment variable overrides it. This is a characterization test of the native chain (it passes immediately); it guards against future regressions in key naming/precedence.

**Files:**
- Create: `tests/Tests/ConfigurationPrecedenceTests.cs`

- [ ] **Step 1: Write the test**

```csharp
using System;
using System.IO;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Verifies the native configuration chain resolves connection strings from the
/// ConnectionStrings block and that environment variables override JSON values,
/// matching the source order appsettings.json -> environment variables.
/// </summary>
[TestFixture]
internal class ConfigurationPrecedenceTests
{
    private const string RedisEnvVariable = "ConnectionStrings__Redis";
    private const string JsonConnectionString = "redis-from-json:6379";

    private const string SampleJson = """
    {
      "ConnectionStrings": {
        "Redis": "redis-from-json:6379"
      }
    }
    """;

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(RedisEnvVariable, null);
    }

    [Test]
    public void GetConnectionString_ResolvesRedisFromJson()
    {
        // Arrange
        var configuration = BuildConfiguration();

        // Act
        var connectionString = configuration.GetConnectionString("Redis");

        // Assert
        connectionString.Should().Be(JsonConnectionString);
    }

    [Test]
    public void EnvironmentVariable_OverridesJsonConnectionString()
    {
        // Arrange
        const string overrideValue = "redis-from-env:6379";
        Environment.SetEnvironmentVariable(RedisEnvVariable, overrideValue);
        var configuration = BuildConfiguration();

        // Act
        var connectionString = configuration.GetConnectionString("Redis");

        // Assert
        connectionString.Should().Be(overrideValue);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)))
            .AddEnvironmentVariables()
            .Build();
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test tests/Tests/Tests.csproj --filter "FullyQualifiedName~ConfigurationPrecedenceTests" --nologo`
Expected: PASS (2 passed). This is a characterization test, so it passes on first run.

- [ ] **Step 3: Commit**

```bash
git add tests/Tests/ConfigurationPrecedenceTests.cs
git commit -m "test: lock in native ConnectionStrings precedence behavior"
```

---

## Task 2: Silo — native config + ConnectionStrings + cluster hydration

**Files:**
- Modify: `src/Silo/Program.cs` (`CreateSilo` ~112-222, `GetRedisEntityRepository` ~224-231)
- Rename: `src/Silo/SiloSettings.json` → `src/Silo/appsettings.json`; `src/Silo/SiloSettings.Release.json` → `src/Silo/appsettings.Release.json` (and `SiloSettings.Debug.json` → `appsettings.Debug.json` if a source copy exists)
- Modify: `src/Silo/Silo.csproj`

- [ ] **Step 1: Rename the settings files (preserve git history)**

```bash
git mv src/Silo/SiloSettings.json src/Silo/appsettings.json
git mv src/Silo/SiloSettings.Release.json src/Silo/appsettings.Release.json
# Only if a source Debug file exists (it may live only under bin/):
git mv src/Silo/SiloSettings.Debug.json src/Silo/appsettings.Debug.json 2>/dev/null || true
```

- [ ] **Step 2: Rewrite `src/Silo/appsettings.json` to use ConnectionStrings**

```json
{
  "LoggingOptions": {
    "LogToConsole": true,
    "LogToDatabase": true,
    "LogLevel": "Information",
    "LogLevelOrleans": "Warning",
    "LoggingIntervalMilliseconds": 15000,
    "SendDetailedErrorsToClient": true
  },
  "ClusterOptions": {
    "Invariant": "Npgsql"
  },
  "ConnectionStrings": {
    "Postgres": "User ID=USERNAME;Password=PASSWORD;Host=resolverpostgres;Port=5432;Database=BtDamageResolver;SSL Mode=Disable;",
    "Redis": "redis:6379,password=PASSWORD"
  },
  "CompressionOptions": {
    "Provider": "Brotli",
    "Quality": 4
  }
}
```

- [ ] **Step 3: Update `src/Silo/Silo.csproj` copy directives**

Replace the two `<ItemGroup>` blocks that reference `SiloSettings.*` with:

```xml
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
    <None Update="appsettings.Release.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <None Update="appsettings.Debug.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 4: Rewrite `CreateSilo()` to read native config inside `UseOrleans((context, siloBuilder) => ...)`**

In `src/Silo/Program.cs`, replace the start of `CreateSilo` (the three local variables + the `Host.CreateDefaultBuilder()...ConfigureAppConfiguration(...).UseOrleans(siloBuilder =>` opening) with the version below. Keep the entire body of the Orleans delegate identical EXCEPT the four changes marked, and the closing stays the same.

```csharp
    private static IHost CreateSilo()
    {
        var (clientPort, siloPort) = GetSiloPortConfigurationFromEnvironment();

        var siloHostBuilder = Host.CreateDefaultBuilder()
            .UseOrleans((context, siloBuilder) =>
            {
                var configuration = context.Configuration;
                var clusterOptions = new FaemiyahClusterOptions
                {
                    Invariant = configuration[$"{Settings.ClusterOptionsBlockName}:Invariant"],
                    ConnectionString = configuration.GetConnectionString("Postgres")
                };

                siloBuilder
                    .Services.AddSerializer(serializerBuilder => serializerBuilder.AddJsonSerializer(
                        isSupported: type => type.Namespace != null && type.Namespace.StartsWith("Faemiyah.BtDamageResolver"),
                        jsonSerializerOptions: CreateJsonSerializerOptions()));
                siloBuilder
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "faemiyah";
                        options.ServiceId = "Resolver";
                    })
                    .Configure<GrainCollectionOptions>(options => options.CollectionAge = TimeSpan.FromDays(1))
                    .Configure<ClusterMembershipOptions>(options =>
                    {
                        options.DefunctSiloExpiration = TimeSpan.FromHours(1);
                        options.NumMissedProbesLimit = 2;
                        options.NumMissedTableIAmAliveLimit = 1;
                        options.NumVotesForDeathDeclaration = 1;
                    })
                    .Configure<SiloMessagingOptions>(options =>
                    {
                        options.MaxRequestProcessingTime = TimeSpan.FromSeconds(15);
                        options.SystemResponseTimeout = TimeSpan.FromSeconds(15);
                    })
                    .Configure<MessagingOptions>(options =>
                    {
                        options.DropExpiredMessages = true;
                        options.ResponseTimeout = TimeSpan.FromSeconds(15);
                        options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(15);
                    })
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = clusterOptions.Invariant;
                        options.ConnectionString = clusterOptions.ConnectionString;
                    })
                    .AddGrainStorage(Settings.ActorStateStoreName, clusterOptions)
                    .AddGrainStorage(Settings.SessionStateStoreName, clusterOptions)
                    .Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = GetHostIp();
                        options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, clientPort);
                        options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);
                    })
                    .AddGrainService<CommunicationService>()
                    .AddGrainService<LoggingService>()
                    .ConfigureServices(services =>
                    {
                        services.ConfigureJsonSerializerOptions();
                        services.Configure<CompressionOptions>(configuration.GetSection(Settings.CompressionOptionsBlockName));
                        services.Configure<FaemiyahClusterOptions>(configuration.GetSection(Settings.ClusterOptionsBlockName));
                        services.PostConfigure<FaemiyahClusterOptions>(options => options.ConnectionString = configuration.GetConnectionString("Postgres"));
                        services.Configure<FaemiyahLoggingOptions>(configuration.GetSection(Settings.LoggingOptionsBlockName));
                        services.AddLogging(conf =>
                        {
                            conf.AddFaemiyahLogging();
                            conf.AddFilter("DeploymentLoadPublisher", LogLevel.Warning);
                        });
                        services.Configure<ConsoleLifetimeOptions>(opt => opt.SuppressStatusMessages = true);
                        services.AddSingleton<ICommunicationServiceClient, CommunicationServiceClient>();
                        services.AddSingleton<IHasher, FaemiyahPasswordHasher>();
                        services.AddSingleton<ILoggingServiceClient, LoggingServiceClient>();
                        services.AddSingleton<ILogicUnitFactory, LogicUnitFactory>();
                        services.AddSingleton<IMathExpression, MathExpression>();
                        services.AddSingleton<IResolverRandom, ResolverRandom>();
                        services.AddSingleton<IEntityRepository<Ammo, string>>(GetRedisEntityRepository<Ammo>);
                        services.AddSingleton<IEntityRepository<ArcDiagram, string>>(GetRedisEntityRepository<ArcDiagram>);
                        services.AddSingleton<IEntityRepository<ClusterTable, string>>(GetRedisEntityRepository<ClusterTable>);
                        services.AddSingleton<IEntityRepository<CriticalDamageTable, string>>(GetRedisEntityRepository<CriticalDamageTable>);
                        services.AddSingleton<IEntityRepository<GameEntry, string>>(GetRedisEntityRepository<GameEntry>);
                        services.AddSingleton<IEntityRepository<PaperDoll, string>>(GetRedisEntityRepository<PaperDoll>);
                        services.AddSingleton<IEntityRepository<Unit, string>>(GetRedisEntityRepository<Unit>);
                        services.AddSingleton<IEntityRepository<Weapon, string>>(GetRedisEntityRepository<Weapon>);
                        services.AddSingleton<CachedEntityRepository<Ammo, string>, CachedEntityRepository<Ammo, string>>();
                        services.AddSingleton<CachedEntityRepository<ArcDiagram, string>, CachedEntityRepository<ArcDiagram, string>>();
                        services.AddSingleton<CachedEntityRepository<ClusterTable, string>, CachedEntityRepository<ClusterTable, string>>();
                        services.AddSingleton<CachedEntityRepository<CriticalDamageTable, string>, CachedEntityRepository<CriticalDamageTable, string>>();
                        services.AddSingleton<CachedEntityRepository<GameEntry, string>, CachedEntityRepository<GameEntry, string>>();
                        services.AddSingleton<CachedEntityRepository<PaperDoll, string>, CachedEntityRepository<PaperDoll, string>>();
                        services.AddSingleton<CachedEntityRepository<Unit, string>, CachedEntityRepository<Unit, string>>();
                        services.AddSingleton<CachedEntityRepository<Weapon, string>, CachedEntityRepository<Weapon, string>>();
                        services.AddSingleton(serviceProvider =>
                        {
                            return new RepositoryProvider(
                                serviceProvider.GetService<CachedEntityRepository<Ammo, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<ArcDiagram, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<ClusterTable, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<CriticalDamageTable, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<PaperDoll, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<Unit, string>>(),
                                serviceProvider.GetService<CachedEntityRepository<Weapon, string>>()
                            );
                        });
                        services.AddSingleton<DataHelper>();
                    });
            });

        return siloHostBuilder.Build();
    }
```

The four changes vs. the original: (1) no top-level `GetConfiguration`/`clusterOptions`; (2) `.ConfigureAppConfiguration(...)` removed; (3) `UseOrleans((context, siloBuilder) => ...)` with `clusterOptions` built from `context.Configuration` + `GetConnectionString("Postgres")`; (4) the `services.Configure<CommunicationOptions>(...)` line is gone and a `FaemiyahClusterOptions` `PostConfigure` is added.

- [ ] **Step 5: Update `GetRedisEntityRepository` to read the Redis connection string from config**

Replace the method body:

```csharp
    private static RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
        where TType : class, IEntity<string>
    {
        var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Redis");
        return !string.IsNullOrEmpty(connectionString)
            ? new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), serviceProvider.GetService<IOptions<JsonSerializerOptions>>(), connectionString)
            : throw new InvalidOperationException($"No 'Redis' connection string configured for entity repository of type {typeof(TType)}.");
    }
```

- [ ] **Step 6: Build the silo**

Run: `dotnet build src/Silo/Silo.csproj --nologo`
Expected: Build succeeded, 0 errors. (`CommunicationOptions` still exists in `Common`; it is simply no longer referenced by the silo.)

- [ ] **Step 7: Commit**

```bash
git add src/Silo/
git commit -m "refactor: silo uses native appsettings + ConnectionStrings"
```

---

## Task 3: Tools — native config via Host.CreateApplicationBuilder

Both tools switch to `Host.CreateApplicationBuilder(args).Configuration`, read Redis from `GetConnectionString("Redis")`, and read their own `appsettings.json`. This also fixes the exporter's cross-file read (it currently loads `DataImporterSettings.json`).

**Files:**
- Modify: `tools/DataImporter/Program.cs`, `tools/DataImporter/DataImporter.cs`, `tools/DataImporter/DataImporter.csproj`
- Modify: `tools/DataExporter/Program.cs`, `tools/DataExporter/DataExporter.cs`, `tools/DataExporter/DataExporter.csproj`
- Rename: `DataImporterSettings.json`/`.Release.json` → `appsettings.json`/`appsettings.Release.json`; same for DataExporter

- [ ] **Step 1: Rename + rewrite the importer settings file**

```bash
git mv tools/DataImporter/DataImporterSettings.json tools/DataImporter/appsettings.json
git mv tools/DataImporter/DataImporterSettings.Release.json tools/DataImporter/appsettings.Release.json
git mv tools/DataImporter/DataImporterSettings.Debug.json tools/DataImporter/appsettings.Debug.json 2>/dev/null || true
```

Rewrite `tools/DataImporter/appsettings.json`:

```json
{
  "LoggingOptions": {
    "LogToConsole": true,
    "LogLevel": "Information",
    "LogLevelOrleans": "Warning"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379,password=PASSWORD"
  }
}
```

- [ ] **Step 2: Rename + rewrite the exporter settings file (drop the dead Postgres block)**

```bash
git mv tools/DataExporter/DataExporterSettings.json tools/DataExporter/appsettings.json
git mv tools/DataExporter/DataExporterSettings.Release.json tools/DataExporter/appsettings.Release.json
git mv tools/DataExporter/DataExporterSettings.Debug.json tools/DataExporter/appsettings.Debug.json 2>/dev/null || true
```

Rewrite `tools/DataExporter/appsettings.json` (identical shape to the importer):

```json
{
  "LoggingOptions": {
    "LogToConsole": true,
    "LogLevel": "Information",
    "LogLevelOrleans": "Warning"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379,password=PASSWORD"
  }
}
```

- [ ] **Step 3: Update both `.csproj` copy directives**

In `tools/DataImporter/DataImporter.csproj` and `tools/DataExporter/DataExporter.csproj`, replace the `SiloSettings`/`DataImporterSettings`/`DataExporterSettings` `None Update` entries with:

```xml
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
    <None Update="appsettings.Release.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <None Update="appsettings.Debug.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 4: Update `tools/DataImporter/Program.cs`**

Replace the private `GetConfiguration` and its call. Change the top of `Main`:

```csharp
        var configuration = Host.CreateApplicationBuilder(args).Configuration;
        var section = configuration.GetSection(Settings.LoggingOptionsBlockName);
```

Delete the entire `private static IConfiguration GetConfiguration(string settingsFile) { ... }` method. Add `using Microsoft.Extensions.Hosting;` and remove `using System.IO;` if no longer used. Keep `using Microsoft.Extensions.Configuration;` (needed for `GetSection`).

- [ ] **Step 5: Update `tools/DataImporter/DataImporter.cs`**

The `DataImporter` constructor takes `ILoggerFactory loggerFactory`. Add an `IConfiguration` parameter so it shares the host configuration, and read Redis from it:

Change the constructor signature and the connection-string lines:

```csharp
    public DataImporter(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<DataImporter>();
        _jsonSerializerOptions = GetJsonSerializerOptions();

        // Repository provider setup
        var redisConnectionString = configuration.GetConnectionString("Redis");
        _repositoryProvider = new RepositoryProvider(
            new RedisEntityRepository<Ammo>(loggerFactory.CreateLogger<RedisEntityRepository<Ammo>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ArcDiagram>(loggerFactory.CreateLogger<RedisEntityRepository<ArcDiagram>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ClusterTable>(loggerFactory.CreateLogger<RedisEntityRepository<ClusterTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<CriticalDamageTable>(loggerFactory.CreateLogger<RedisEntityRepository<CriticalDamageTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<PaperDoll>(loggerFactory.CreateLogger<RedisEntityRepository<PaperDoll>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Unit>(loggerFactory.CreateLogger<RedisEntityRepository<Unit>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Weapon>(loggerFactory.CreateLogger<RedisEntityRepository<Weapon>>(), Options.Create(_jsonSerializerOptions), redisConnectionString));
    }
```

KEEP `using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;` (still used by `GetJsonSerializerOptions()`). Remove `using Faemiyah.BtDamageResolver.Common.Options;` (no longer references `CommunicationOptions`) and `using Faemiyah.BtDamageResolver.Common.Constants;` (the only `Settings.*` use — `CommunicationOptionsBlockName` — is gone). Keep `using Microsoft.Extensions.Configuration;`.

- [ ] **Step 6: Thread configuration through the importer's `RunDataImport`**

In `tools/DataImporter/Program.cs`, the `RunDataImport` helper constructs `new DataImporter(loggerFactory)`. Pass configuration through. Update `Main` to capture `configuration` (already done in Step 4) and change `RunDataImport`:

```csharp
        var result = Parser.Default.ParseArguments<DataImportOptions>(args)
            .MapResult(
                initOptions => RunDataImport(loggerFactory, configuration, initOptions).Result,
                errs => 1);
```

```csharp
    private static async Task<int> RunDataImport(ILoggerFactory loggerFactory, IConfiguration configuration, DataImportOptions options)
    {
        try
        {
            var dataImporter = new DataImporter(loggerFactory, configuration);
            await dataImporter.Work(options);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }

        return 0;
    }
```

- [ ] **Step 7: Apply the identical changes to the exporter**

Repeat Steps 4-6 for `tools/DataExporter/Program.cs` and `tools/DataExporter/DataExporter.cs`, substituting `DataExporter`/`DataExportOptions`/`RunDataExport` and the logger category `"DataExporter"`. The two `Program.cs` files are structurally identical apart from those names. Apply the same `using` cleanup to `DataExporter.cs` (remove `Common.Options` and `Common.Constants`, keep the `ConfigurationUtilities` static using). The exporter's `DataExporter` constructor (currently reading `DataImporterSettings.json`) becomes:

```csharp
    public DataExporter(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<DataExporter>();
        _jsonSerializerOptions = GetJsonSerializerOptions();

        // Pretty printing options
        _jsonSerializerOptions.IndentCharacter = ' ';
        _jsonSerializerOptions.IndentSize = 4;
        _jsonSerializerOptions.WriteIndented = true;

        // Repository provider setup
        var redisConnectionString = configuration.GetConnectionString("Redis");
        _repositoryProvider = new RepositoryProvider(
            new RedisEntityRepository<Ammo>(loggerFactory.CreateLogger<RedisEntityRepository<Ammo>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ArcDiagram>(loggerFactory.CreateLogger<RedisEntityRepository<ArcDiagram>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ClusterTable>(loggerFactory.CreateLogger<RedisEntityRepository<ClusterTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<CriticalDamageTable>(loggerFactory.CreateLogger<RedisEntityRepository<CriticalDamageTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<PaperDoll>(loggerFactory.CreateLogger<RedisEntityRepository<PaperDoll>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Unit>(loggerFactory.CreateLogger<RedisEntityRepository<Unit>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Weapon>(loggerFactory.CreateLogger<RedisEntityRepository<Weapon>>(), Options.Create(_jsonSerializerOptions), redisConnectionString));
    }
```

- [ ] **Step 8: Build both tools**

Run: `dotnet build tools/DataImporter/DataImporter.csproj --nologo && dotnet build tools/DataExporter/DataExporter.csproj --nologo`
Expected: Build succeeded, 0 errors.

- [ ] **Step 9: Commit**

```bash
git add tools/
git commit -m "refactor: tools use native config + ConnectionStrings; fix exporter cross-file read"
```

---

## Task 4: Services — CommunicationService reads Redis from IConfiguration

**Files:**
- Modify: `src/Services/CommunicationService.cs`

- [ ] **Step 1: Swap the options dependency for `IConfiguration`**

Change the constructor parameter `IOptions<CommunicationOptions> communicationOptions` to `IConfiguration configuration`, and line 44's `communicationOptions.Value.ConnectionString` to `configuration.GetConnectionString("Redis")`:

```csharp
    public CommunicationService(
        ILogger<CommunicationService> logger,
        IConfiguration configuration,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        DataHelper dataHelper,
        IGrainFactory grainFactory,
        GrainId grainId,
        Silo silo,
        ILoggerFactory loggerFactory) : base(grainId, silo, loggerFactory)
    {
        _logger = logger;
        _serverToClientCommunicator = new ServerToClientCommunicator(loggerFactory.CreateLogger<ServerToClientCommunicator>(), jsonSerializerOptions, configuration.GetConnectionString("Redis"), dataHelper, grainFactory);
    }
```

Update the `<param name="communicationOptions">` doc line to `<param name="configuration">The application configuration.</param>`. Add `using Microsoft.Extensions.Configuration;`. Remove `using Faemiyah.BtDamageResolver.Common.Options;` (keep `using Microsoft.Extensions.Options;` — still used by `IOptions<JsonSerializerOptions>`).

- [ ] **Step 2: Build Services**

Run: `dotnet build src/Services/Services.csproj --nologo`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Services/CommunicationService.cs
git commit -m "refactor: CommunicationService reads Redis from IConfiguration"
```

---

## Task 5: Delete CommunicationOptions, GetConfiguration, RESOLVER_ENVIRONMENT; fix Settings docs

At this point nothing in the code repo references `CommunicationOptions` or `GetConfiguration`. Remove them.

**Files:**
- Delete: `src/Common/Options/CommunicationOptions.cs`
- Modify: `src/Common/Constants/Settings.cs`
- Modify: `src/Common/ConfigurationUtilities.cs`

- [ ] **Step 1: Delete the options class**

```bash
git rm src/Common/Options/CommunicationOptions.cs
```

- [ ] **Step 2: Update `src/Common/Constants/Settings.cs`**

Remove the `CommunicationOptionsBlockName` constant and its doc comment. Fix the two wrong "RabbitMQ" doc comments:

```csharp
    /// <summary>
    /// The name of the options block which contains Orleans clustering settings (the ADO.NET invariant).
    /// </summary>
    public const string ClusterOptionsBlockName = "ClusterOptions";

    /// <summary>
    /// The name of the options block which configures application logging.
    /// </summary>
    public const string LoggingOptionsBlockName = "LoggingOptions";
```

(Leave `CompressionOptionsBlockName`, `ActorStateStoreName`, `SessionStateStoreName`, `MaximumGameEntryAgeHours` unchanged.)

- [ ] **Step 3: Trim `src/Common/ConfigurationUtilities.cs`**

Delete the `GetConfiguration` method (lines ~80-118), the `GetEnvironmentName` method (lines ~64-78), and the `EnvironmentName` / `DefaultDebugEnvironmentName` / `DefaultEnvironmentName` constants. Simplify `GetSiloPortConfigurationFromEnvironment` to drop the `portPad` parameter and the Debug-padding block:

```csharp
    public static (int ClientPort, int SiloPort) GetSiloPortConfigurationFromEnvironment()
    {
        int clientPort = DefaultOrleansClientPort, siloPort = DefaultOrleansSiloPort;

        try
        {
            var envClientPort = Environment.GetEnvironmentVariable(EnvironmentOrleansClientPort);
            var envSiloPort = Environment.GetEnvironmentVariable(EnvironmentOrleansSiloPort);

            if (envClientPort != null && envSiloPort != null)
            {
                clientPort = int.Parse(envClientPort, NumberStyles.Integer, CultureInfo.InvariantCulture);
                siloPort = int.Parse(envSiloPort, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to resolve silo port configuration from environment variables.", ex);
        }

        return (clientPort, siloPort);
    }
```

Remove the now-unused `using System.IO;` and `using Microsoft.Extensions.Configuration;` directives. Keep `System`, `System.Globalization`, `System.Net`, `System.Net.Sockets`, `System.Text.Json`, `System.Text.Json.Serialization`, `Microsoft.Extensions.DependencyInjection`. (`Faemiyah...Common.Options` is no longer needed either — remove it.)

- [ ] **Step 4: Full code-repo build + tests**

Run: `dotnet build --nologo`
Expected: Build succeeded, 0 errors across all projects (Common, Api, Services, Actors, Silo, tools, Tests).

Run: `dotnet test tests/Tests/Tests.csproj --nologo`
Expected: All tests pass (including `ConfigurationPrecedenceTests`).

- [ ] **Step 5: Verify RESOLVER_ENVIRONMENT is gone from code**

Run: `git grep -n "RESOLVER_ENVIRONMENT\|CommunicationOptions\|GetConfiguration\|GetEnvironmentName"`
Expected: no matches in `src/`, `tools/`, or `tests/` (matches only inside `docs/superpowers/`).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: remove CommunicationOptions, custom config loader, RESOLVER_ENVIRONMENT"
```

---

## Task 6: Infra — rename orleansdb → resolverpostgres + ConnectionStrings env vars

**Files (in the infra repo — `git status` to confirm which repo tracks these):**
- Modify: `infra/docker-compose.yml`
- Modify: `infra/grafana/datasources/datasource.yml`
- Modify: `refresh.sh`, `refresh.bat`

- [ ] **Step 1: Update `infra/docker-compose.yml`**

Make these edits:
- `resolver.environment`: `RESOLVER_ENVIRONMENT: Release` → `DOTNET_ENVIRONMENT: Release`; rename `ClusterOptions__ConnectionString` → `ConnectionStrings__Postgres` and change `Host=orleansdb` → `Host=resolverpostgres`; rename `CommunicationOptions__ConnectionString` → `ConnectionStrings__Redis`.
- `resolver.depends_on`: `- orleansdb` → `- resolverpostgres`.
- `resolverclient.environment`: remove the `RESOLVER_ENVIRONMENT: Release` line; rename `CommunicationOptions__ConnectionString` → `ConnectionStrings__Redis` (keep `ASPNETCORE_ENVIRONMENT: Release`).
- `resolverdataimporter.environment`: `RESOLVER_ENVIRONMENT: Release` → `DOTNET_ENVIRONMENT: Release`; rename `CommunicationOptions__ConnectionString` → `ConnectionStrings__Redis`.
- `grafana.depends_on`: `- orleansdb` → `- resolverpostgres`.
- The `orleansdb:` service block → rename the key to `resolverpostgres:`; `container_name: orleansdb` → `resolverpostgres`; `hostname: orleansdb` → `resolverpostgres`; `image: orleansdb:latest` → `resolverpostgres:latest`; volume mount `orleansdb-data:/var/lib/postgresql/18/docker` → `resolverpostgres-data:/var/lib/postgresql/18/docker`.
- Top-level `volumes:` block: `orleansdb-data:` → `resolverpostgres-data:`.

Resulting `resolver` env block for reference:

```yaml
    environment:
      DOTNET_ENVIRONMENT: Release
      ConnectionStrings__Postgres: "User ID=${RESOLVER_USER:?err};Password=${RESOLVER_PASSWORD:?err};Host=resolverpostgres;Port=5432;Database=BtDamageResolver;SSL Mode=Disable;"
      ConnectionStrings__Redis: "redis:6379,password=${RESOLVER_PASSWORD:?err}"
```

- [ ] **Step 2: Update `infra/grafana/datasources/datasource.yml`**

Change `url: orleansdb:5432` → `url: resolverpostgres:5432`. Leave `name: Orleansdb` unchanged (Grafana dashboards may reference the datasource by name; renaming the display name is out of scope).

- [ ] **Step 3: Update build scripts**

In `refresh.sh` and `refresh.bat`, change `docker build --tag orleansdb -f ./postgresql/Dockerfile ./postgresql/` → `docker build --tag resolverpostgres -f ./postgresql/Dockerfile ./postgresql/`.

- [ ] **Step 4: Verify no stray references**

Run: `git grep -n "orleansdb"` (in the infra repo)
Expected: no matches (the volume, hostname, image, datasource url, and build tag are all renamed).

- [ ] **Step 5: Commit (in the infra repo)**

```bash
git add infra/docker-compose.yml infra/grafana/datasources/datasource.yml refresh.sh refresh.bat
git commit -m "chore: rename orleansdb container to resolverpostgres; native ConnectionStrings env vars"
```

---

## Task 7: Client — native config + ConnectionStrings (separate repo)

**Files (in `C:\Work\src\BtDamageResolver\BtDamageResolverClient`):**
- Modify: `src/BlazorServer/Startup.cs`
- Modify: `src/BlazorServer/Communication/ResolverCommunicator.cs`
- Modify: `src/BlazorServer/appsettings.json`
- Create: `src/BlazorServer/appsettings.Release.json`
- Delete: `src/BlazorServer/CommunicationSettings.json`, `CommunicationSettings.Release.json`, `CommunicationSettings.Debug.json`
- Modify: `src/BlazorServer/BlazorServer.csproj`

- [ ] **Step 1: Merge config into `appsettings.json`**

Rewrite `src/BlazorServer/appsettings.json` to fold in the contents of `CommunicationSettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "LoggingOptions": {
    "LogToConsole": true,
    "LogLevel": "Information",
    "LogLevelOrleans": "Warning"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379,password=PASSWORD"
  },
  "CompressionOptions": {
    "Provider": "Brotli",
    "Quality": 4
  }
}
```

- [ ] **Step 2: Move the Release override and delete the old files**

```bash
git mv src/BlazorServer/CommunicationSettings.Release.json src/BlazorServer/appsettings.Release.json
git rm src/BlazorServer/CommunicationSettings.json src/BlazorServer/CommunicationSettings.Debug.json
```

If `appsettings.Release.json` is empty `{}`, leave it as `{}`.

- [ ] **Step 3: Update `BlazorServer.csproj`**

Remove the two `<ItemGroup>` blocks that reference `CommunicationSettings.*`. Add an explicit copy for the Release override (the Web SDK auto-includes `appsettings.json`/`appsettings.Development.json` but not `appsettings.Release.json`):

```xml
  <ItemGroup>
    <None Update="appsettings.Release.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 4: Make `ConfigureServices` an instance method binding off `Configuration`**

In `src/BlazorServer/Startup.cs`, change the signature from `public static void ConfigureServices(IServiceCollection services)` to `public void ConfigureServices(IServiceCollection services)`, delete the line `var configuration = GetConfiguration("CommunicationSettings.json");`, and replace every `configuration.` reference in the method with `Configuration.` (the injected property). Specifically:

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CompressionOptions>(Configuration.GetSection(Settings.CompressionOptionsBlockName));
        services.Configure<FaemiyahLoggingOptions>(Configuration.GetSection(Settings.LoggingOptionsBlockName));
```

Update the data-protection line that read `configuration[...]`:

```csharp
        services.AddDataProtection().SetApplicationName("BtDamageResolverClient").PersistKeysToFileSystem(new DirectoryInfo(Configuration["DataProtectionKeysPath"] ?? "/app/dpkeys/"));
```

Delete the now-removed `services.Configure<CommunicationOptions>(...)` line. Remove `using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;` ONLY if `ConfigureJsonSerializerOptions()` (line ~89) is the sole remaining use — it is an extension method on `IServiceCollection` from that static class, so KEEP the static using. Leave the rest of the method unchanged.

- [ ] **Step 5: Update `GetRedisEntityRepository` in `Startup.cs`**

```csharp
    private RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
        where TType : class, IEntity<string>
    {
        var connectionString = Configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(connectionString))
        {
            return new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), serviceProvider.GetService<IOptions<JsonSerializerOptions>>(), connectionString);
        }

        throw new InvalidOperationException($"No 'Redis' connection string configured for entity repository of type {typeof(TType)}.");
    }
```

(`GetRedisEntityRepository` becomes a non-static instance method so it can read `Configuration`; it is referenced as a method-group `GetRedisEntityRepository<Ammo>` in the registrations, which still resolves on the instance.)

- [ ] **Step 6: Update `ResolverCommunicator.cs`**

Replace the `IOptions<CommunicationOptions>` dependency with `IConfiguration`:

```csharp
    private readonly string _redisConnectionString;
```

(replacing `private readonly CommunicationOptions _communicationOptions;`)

```csharp
    public ResolverCommunicator(ILogger<ResolverCommunicator> logger, IConfiguration configuration, IOptions<JsonSerializerOptions> jsonSerializerOptions, DataHelper dataHelper, HubConnection hubConnection)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerOptions;
        _redisConnectionString = configuration.GetConnectionString("Redis");
        _dataHelper = dataHelper;
        _hubConnection = hubConnection;
    }
```

At line ~368, change `_communicationOptions.ConnectionString` → `_redisConnectionString`. Update the `<param name="communicationOptions">` doc to `<param name="configuration">The application configuration.</param>`. Add `using Microsoft.Extensions.Configuration;`. Remove `using Faemiyah.BtDamageResolver.Common.Options;` (keep `Microsoft.Extensions.Options` — still used for `IOptions<JsonSerializerOptions>`).

- [ ] **Step 7: Build the client**

Run (from the client repo root): `dotnet build src/BlazorServer/BlazorServer.csproj --nologo`
Expected: Build succeeded, 0 errors. (The client compiles against `Common` NuGet 0.0.447, which still contains `CommunicationOptions`; the client simply no longer references it.)

- [ ] **Step 8: Commit (in the client repo)**

```bash
git add -A
git commit -m "refactor: client uses native appsettings + ConnectionStrings"
```

---

## Final verification (manual gate — owner)

- [ ] Code repo: `dotnet build --nologo` and `dotnet test tests/Tests/Tests.csproj --nologo` both green.
- [ ] Client repo: `dotnet build src/BlazorServer/BlazorServer.csproj --nologo` green.
- [ ] `git grep -n "orleansdb"` clean in code + infra repos.
- [ ] Build images (`./refresh.sh` or `refresh.bat`) and `docker compose -f infra/docker-compose.yml up`. Confirm: silo connects to Redis + Postgres, client connects, DB logging writes. Note the Postgres volume is reset (empty DB; schema recreated by the custom image on first init).
```
