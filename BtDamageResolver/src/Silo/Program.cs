using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Cryptography;
using Faemiyah.BtDamageResolver.Actors.Logic;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Faemiyah.BtDamageResolver.Services;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Silo;

/// <summary>
/// Main program class.
/// </summary>
public static class Program
{
    private static readonly ManualResetEvent SiloStopped = new(false);
    private static readonly object SyncLock = new();
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static IHost _siloHost;
    private static bool _siloStopping;

    /// <summary>
    /// Main entry point.
    /// </summary>
    public static void Main()
    {
        SetupApplicationShutdown();

        _siloHost = CreateSilo();

        try
        {
            _siloHost.StartAsync(CancellationTokenSource.Token).Wait(CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Thor Gateway RF silo startup interrupted. Shutting down.");
        }

        // Wait for the silo to completely shutdown before exiting.
        SiloStopped.WaitOne();
    }

    private static void SetupApplicationShutdown()
    {
        // Capture the user pressing Ctrl+C
        Console.CancelKeyPress += (_, a) =>
        {
            // Prevent the application from crashing ungracefully.
            a.Cancel = true;

            // Don't allow the following code to repeat if the user presses Ctrl+C repeatedly.
            lock (SyncLock)
            {
                if (!_siloStopping)
                {
                    _siloStopping = true;
                    Task.Run(StopSilo).Ignore();
                }
            }

            // Event handler execution exits immediately, leaving the silo shutdown running on a background thread,
            // but the app doesn't crash because a.Cancel has been set = true
        };
    }

    private static async Task StopSilo()
    {
        try
        {
            // Allow 30 to finish up gracefully, otherwise shut down forcefully.
            await _siloHost.StopAsync(new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            // If silo is in starting state, cancel the cancellation token to interrupt it.
            CancellationTokenSource.Cancel();
        }

        SiloStopped.Set();
    }

    private static IHost CreateSilo()
    {
        var (clientPort, siloPort) = GetSiloPortConfigurationFromEnvironment();
        var configuration = GetConfiguration("SiloSettings.json");

        var clusterOptions = configuration.GetSection(Settings.ClusterOptionsBlockName).Get<FaemiyahClusterOptions>();
        Console.WriteLine($"CONNECTION STRING: {clusterOptions?.ConnectionString}");

        var siloHostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) => { config.AddConfiguration(configuration); })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder
                    .Services.AddSerializer(serializerBuilder =>
                    {
                        serializerBuilder.AddNewtonsoftJsonSerializer(isSupported: type => type.Namespace != null && type.Namespace.StartsWith("Faemiyah.BtDamageResolver"));
                    });
                siloBuilder
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "faemiyah";
                        options.ServiceId = "Resolver";
                    })
                    .Configure<GrainCollectionOptions>(options => { options.CollectionAge = TimeSpan.FromDays(1); })
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
                    .Configure<JsonSerializerSettings>(options =>
                    {
                        options.NullValueHandling = NullValueHandling.Ignore;
                        options.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    })
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = clusterOptions?.Invariant;
                        options.ConnectionString = clusterOptions?.ConnectionString;
                    })
                    .AddGrainStorage(Settings.ActorStateStoreName, clusterOptions)
                    .AddGrainStorage(Settings.SessionStateStoreName, clusterOptions)
                    .Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = GetHostIp();

                        // The socket used for silo-to-silo will bind to this endpoint
                        options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, clientPort);

                        // The socket used by the gateway will bind to this endpoint
                        options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);
                    })
                    .AddGrainService<CommunicationService>()
                    .AddGrainService<LoggingService>()
                    .ConfigureServices(services =>
                    {
                        services.Configure<CommunicationOptions>(configuration.GetSection(Settings.CommunicationOptionsBlockName));
                        services.Configure<FaemiyahClusterOptions>(configuration.GetSection(Settings.ClusterOptionsBlockName));
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
                        services.AddSingleton<IEntityRepository<ClusterTable, string>>(GetRedisEntityRepository<ClusterTable>);
                        services.AddSingleton<IEntityRepository<CriticalDamageTable, string>>(GetRedisEntityRepository<CriticalDamageTable>);
                        services.AddSingleton<IEntityRepository<GameEntry, string>>(GetRedisEntityRepository<GameEntry>);
                        services.AddSingleton<IEntityRepository<PaperDoll, string>>(GetRedisEntityRepository<PaperDoll>);
                        services.AddSingleton<IEntityRepository<Unit, string>>(GetRedisEntityRepository<Unit>);
                        services.AddSingleton<IEntityRepository<Weapon, string>>(GetRedisEntityRepository<Weapon>);
                        services.AddSingleton<CachedEntityRepository<Ammo, string>, CachedEntityRepository<Ammo, string>>();
                        services.AddSingleton<CachedEntityRepository<ClusterTable, string>, CachedEntityRepository<ClusterTable, string>>();
                        services.AddSingleton<CachedEntityRepository<CriticalDamageTable, string>, CachedEntityRepository<CriticalDamageTable, string>>();
                        services.AddSingleton<CachedEntityRepository<GameEntry, string>, CachedEntityRepository<GameEntry, string>>();
                        services.AddSingleton<CachedEntityRepository<PaperDoll, string>, CachedEntityRepository<PaperDoll, string>>();
                        services.AddSingleton<CachedEntityRepository<Unit, string>, CachedEntityRepository<Unit, string>>();
                        services.AddSingleton<CachedEntityRepository<Weapon, string>, CachedEntityRepository<Weapon, string>>();
                    });
                    /*.AddStartupTask((serviceProvider, _) =>
                    {
                        var ammoRepository = serviceProvider.GetService<CachedEntityRepository<Ammo, string>>();
                        var clusterTableRepository = serviceProvider.GetService<CachedEntityRepository<ClusterTable, string>>();
                        var criticalDamageRepository = serviceProvider.GetService<CachedEntityRepository<CriticalDamageTable, string>>();
                        var paperDollRepository = serviceProvider.GetService<CachedEntityRepository<PaperDoll, string>>();
                        var unitRepository = serviceProvider.GetService<CachedEntityRepository<Unit, string>>();
                        var weaponRepository = serviceProvider.GetService<CachedEntityRepository<Weapon, string>>();
                        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("Startup");
                        logger.LogInformation("AmmoRepository ready with {n} items.", ammoRepository.GetAll().Count);
                        logger.LogInformation("CriticalDamageRepository ready with {n} items.", clusterTableRepository.GetAll().Count);
                        logger.LogInformation("CriticalDamageRepository ready with {n} items.", criticalDamageRepository.GetAll().Count);
                        logger.LogInformation("PaperDollRepository ready with {n} items.", paperDollRepository.GetAll().Count);
                        logger.LogInformation("UnitRepository ready with {n} items.", unitRepository.GetAll().Count);
                        logger.LogInformation("WeaponRepository ready with {n} items.", weaponRepository.GetAll().Count);

                        return Task.CompletedTask;
                    });*/
            });

        return siloHostBuilder.Build();
    }

    private static RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
        where TType : class, IEntity<string>
    {
        var options = serviceProvider.GetService<IOptions<CommunicationOptions>>();
        return options != null
            ? new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), serviceProvider.GetService<IOptions<JsonSerializerSettings>>(), options.Value.ConnectionString)
            : throw new InvalidOperationException($"Unable to resolve options class providing connection string for entity repository of type {typeof(TType)}.");
    }

    /*private static SqlEntityRepository<TType> GetSqlEntityRepository<TType>(IServiceProvider serviceProvider) where TType : class, IEntity<string>
    {
        var options = serviceProvider.GetService<IOptions<FaemiyahClusterOptions>>();
        if (options != null)
        {
            return new SqlEntityRepository<TType>(serviceProvider.GetService<ILogger<SqlEntityRepository<TType>>>(), options.Value.ConnectionString);
        }

        throw new InvalidOperationException($"Unable to resolve options class providing connection string for entity repository of type {typeof(TType)}.");
    }*/

    private static ISiloBuilder AddGrainStorage(this ISiloBuilder siloHostBuilder, string name, FaemiyahClusterOptions clusterOptions)
    {
        siloHostBuilder.AddAdoNetGrainStorage(name, options =>
        {
            options.Invariant = clusterOptions.Invariant;
            options.ConnectionString = clusterOptions.ConnectionString;
        });

        return siloHostBuilder;
    }
}