using System;
using System.Text.Json;
using Blazored.LocalStorage;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR.Client;
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
    public static void ConfigureServices(IServiceCollection services)
    {
        var configuration = GetConfiguration("CommunicationSettings.json");

        services.Configure<CommunicationOptions>(configuration.GetSection(Settings.CommunicationOptionsBlockName));
        services.Configure<FaemiyahLoggingOptions>(configuration.GetSection(Settings.LoggingOptionsBlockName));
        services.Configure<CircuitOptions>(options =>
        {
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromHours(1);
        })
        .Configure<HttpConnectionDispatcherOptions>(options =>
        {
            options.ApplicationMaxBufferSize = 1048576;
            options.TransportMaxBufferSize = 1048576;
        });
        services.AddBlazoredLocalStorage();
        services.AddLogging(conf =>
        {
            conf.AddFaemiyahLogging();
        });

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1048576;
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
        services.AddScoped<ClientHub>();
        services.AddScoped<LocalStorage>();
        services.AddScoped<ResolverCommunicator>();
        services.AddScoped<UserStateController>();
        services.AddScoped<HubConnection>(serviceProvider =>
        {
            var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
            return new HubConnectionBuilder()
            .WithAutomaticReconnect()
                .WithUrl(navigationManager.ToAbsoluteUri("/ClientHub"))
                .Build();
        });
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
            endpoints.MapHub<ClientHub>("/ClientHub");
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    private static RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider)
        where TType : class, IEntity<string>
    {
        var options = serviceProvider.GetService<IOptions<CommunicationOptions>>();

        if (options != null)
        {
            return new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), serviceProvider.GetService<IOptions<JsonSerializerOptions>>(), options.Value.ConnectionString);
        }

        throw new InvalidOperationException($"Unable to resolve options class providing connection string for entity repository of type {typeof(TType)}.");
    }
}