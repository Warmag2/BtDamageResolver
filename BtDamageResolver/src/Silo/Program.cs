using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Silo
{
    /// <summary>
    /// Main program class.
    /// </summary>
    public static class Program
    {
        private static readonly ManualResetEvent SiloStopped = new(false);
        private static readonly object SyncLock = new();
        private static readonly CancellationTokenSource CancellationTokenSource = new();
        private static ISiloHost _siloHost;
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

        private static ISiloHost CreateSilo()
        {
            var (clientPort, siloPort) = GetSiloPortConfigurationFromEnvironment();
            var configuration = GetConfiguration("SiloSettings.json");

            var clusterOptions = configuration.GetSection(Settings.ClusterOptionsBlockName).Get<FaemiyahClusterOptions>();
            Console.WriteLine($"CONNECTION STRING: {clusterOptions.ConnectionString}");

            var siloHostBuilder = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "faemiyah";
                    options.ServiceId = "Resolver";
                })
                .Configure<GrainCollectionOptions>(options =>
                {
                    options.CollectionAge = TimeSpan.FromDays(1);
                })
                .Configure<ClusterMembershipOptions>(options =>
                {
                    options.DefunctSiloCleanupPeriod = TimeSpan.FromMinutes(15);
                    options.DefunctSiloExpiration = TimeSpan.FromMinutes(5);
                    options.ValidateInitialConnectivity = false;
                    options.NumMissedProbesLimit = 3;
                    options.NumVotesForDeathDeclaration = 1;
                })
                .Configure<SiloMessagingOptions>(options =>
                {
                    options.ClientDropTimeout = TimeSpan.FromHours(1);
                    options.MaxRequestProcessingTime = TimeSpan.FromMinutes(1);
                })
                .Configure<MessagingOptions>(options =>
                {
                    options.MaxMessageBodySize = 1048576;
                    options.MaxMessageHeaderSize = 1048576;
                    options.ResponseTimeout = TimeSpan.FromMinutes(1);
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

                    // The socket used for silo-to-silo will bind to this endpoint
                    options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, clientPort);

                    // The socket used by the gateway will bind to this endpoint
                    options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);
                })
                .ConfigureAppConfiguration((_, config) => { config.AddConfiguration(configuration); })
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
                    services.AddSingleton<ILogicUnitFactory, LogicUnitFactory>();
                    services.AddSingleton<IResolverRandom, ResolverRandom>();
                    services.AddSingleton<IMathExpression, MathExpression>();
                    services.AddSingleton<ICommunicationServiceClient, CommunicationServiceClient>();
                    services.AddSingleton<ILoggingServiceClient, LoggingServiceClient>();
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

            return siloHostBuilder.Build();
        }

        private static RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
            where TType : class, IEntity<string>
        {
            var options = serviceProvider.GetService<IOptions<CommunicationOptions>>();
            if (options != null)
            {
                return new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), options.Value.ConnectionString);
            }

            throw new InvalidOperationException($"Unable to resolve options class providing connection string for entity repository of type {typeof(TType)}.");
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

        private static ISiloHostBuilder AddGrainStorage(this ISiloHostBuilder siloHostBuilder, string name, FaemiyahClusterOptions clusterOptions)
        {
            siloHostBuilder.AddAdoNetGrainStorage(name, options =>
            {
                options.UseJsonFormat = true;
                options.IndentJson = true;
                options.UseXmlFormat = false;
                options.Invariant = clusterOptions.Invariant;
                options.ConnectionString = clusterOptions.ConnectionString;
                options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
            });

            return siloHostBuilder;
        }

        private static void ConfigureJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.Culture = CultureInfo.InvariantCulture;
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        }
    }
}
