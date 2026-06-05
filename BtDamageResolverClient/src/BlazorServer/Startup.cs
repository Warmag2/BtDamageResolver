using System;
using System.IO;
using System.Text.Json;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Logging.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer;

/// <summary>
/// Startup class for service.
/// </summary>
public class Startup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The configuration to use.</param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// The service configuration.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// For more information on how to configure your application, see https://go.microsoft.com/fwlink/?LinkID=398940.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CompressionOptions>(Configuration.GetSection(Settings.CompressionOptionsBlockName));
        services.Configure<FaemiyahLoggingOptions>(Configuration.GetSection(Settings.LoggingOptionsBlockName));
        services.Configure<CircuitOptions>(options =>
        {
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromHours(1);

            // Each retained disconnected circuit keeps its scoped Redis subscription alive and
            // continues processing its game's messages, so this cap bounds background CPU as well
            // as memory. Raised to 256 to allow many simultaneously-paused phone clients to reconnect
            // to their existing circuit within the retention window.
            options.DisconnectedCircuitMaxRetained = 256;
        })
        .Configure<HttpConnectionDispatcherOptions>(options =>
        {
            options.ApplicationMaxBufferSize = 1048576;
            options.TransportMaxBufferSize = 1048576;
        });
        services.AddLogging(conf =>
        {
            conf.AddFaemiyahLogging();
        });

        services.AddDataProtection().SetApplicationName("BtDamageResolverClient").PersistKeysToFileSystem(new DirectoryInfo(Configuration["DataProtectionKeysPath"] ?? "/app/dpkeys/"));
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddSignalR(options =>
        {
            // Detailed SignalR errors echo server-side exception messages to the client, so only
            // enable them outside of Production to avoid leaking internal details.
            options.EnableDetailedErrors = !string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Environments.Production,
                StringComparison.OrdinalIgnoreCase);
        });
        services.ConfigureJsonSerializerOptions();
        services.AddSingleton<IEntityRepository<Ammo, string>>(GetRedisEntityRepository<Ammo>);
        services.AddSingleton<IEntityRepository<ArcDiagram, string>>(GetRedisEntityRepository<ArcDiagram>);
        services.AddSingleton<IEntityRepository<ClusterTable, string>>(GetRedisEntityRepository<ClusterTable>);
        services.AddSingleton<IEntityRepository<CriticalDamageTable, string>>(GetRedisEntityRepository<CriticalDamageTable>);
        services.AddSingleton<IEntityRepository<GameEntry, string>>(GetRedisEntityRepository<GameEntry>);
        services.AddSingleton<IEntityRepository<PaperDoll, string>>(GetRedisEntityRepository<PaperDoll>);
        services.AddSingleton<IEntityRepository<Unit, string>>(GetRedisEntityRepository<Unit>);
        services.AddSingleton<IEntityRepository<Weapon, string>>(GetRedisEntityRepository<Weapon>);
        services.AddSingleton<CommonData>();
        services.AddSingleton<DataHelper>();
        services.AddScoped<LocalStorage>();
        services.AddScoped<ClientMessageDispatcher>();
        services.AddScoped(serviceProvider => new ResolverCommunicator(
            serviceProvider.GetRequiredService<ILogger<ResolverCommunicator>>(),
            Configuration.GetConnectionString(Settings.RedisConnectionStringName)
                ?? throw new InvalidOperationException($"No '{Settings.RedisConnectionStringName}' connection string configured."),
            serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>(),
            serviceProvider.GetRequiredService<DataHelper>(),
            serviceProvider.GetRequiredService<ClientMessageDispatcher>()));
        services.AddScoped<UserStateController>();
    }

    /// <summary>
    /// Configure service.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The web host environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAntiforgery();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    private RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
        where TType : class, IEntity<string>
    {
        var connectionString = Configuration.GetConnectionString(Settings.RedisConnectionStringName);

        if (!string.IsNullOrEmpty(connectionString))
        {
            return new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), serviceProvider.GetService<IOptions<JsonSerializerOptions>>(), connectionString);
        }

        throw new InvalidOperationException($"No 'Redis' connection string configured for entity repository of type {typeof(TType)}.");
    }
}