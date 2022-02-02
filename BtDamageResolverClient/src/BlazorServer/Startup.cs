using System;
using Blazored.LocalStorage;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = GetConfiguration("CommunicationSettings.json");
            //var clusterOptions = configuration.GetSection("ClusterOptions").Get<FaemiyahClusterOptions>();
            //var client = ConnectClient(clusterOptions);
            //var commonData = new CommonData(client);

            services.Configure<CommunicationOptions>(configuration.GetSection(Settings.CommunicationOptionsBlockName));
            services.Configure<FaemiyahLoggingOptions>(configuration.GetSection(Settings.LoggingOptionsBlockName));
            services.Configure<CircuitOptions>(options =>
            {
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromDays(1);
            });
            services.Configure<HttpConnectionDispatcherOptions>(options =>
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
            services.AddSingleton<IEntityRepository<Ammo, string>>(GetRedisEntityRepository<Ammo>);
            services.AddSingleton<IEntityRepository<ClusterTable, string>>(GetRedisEntityRepository<ClusterTable>);
            services.AddSingleton<IEntityRepository<CriticalDamageTable, string>>(GetRedisEntityRepository<CriticalDamageTable>);
            services.AddSingleton<IEntityRepository<GameEntry, string>>(GetRedisEntityRepository<GameEntry>);
            services.AddSingleton<IEntityRepository<PaperDoll, string>>(GetRedisEntityRepository<PaperDoll>);
            services.AddSingleton<IEntityRepository<Unit, string>>(GetRedisEntityRepository<Unit>);
            services.AddSingleton<IEntityRepository<Weapon, string>>(GetRedisEntityRepository<Weapon>);
            services.AddSingleton<CommonData>();
            services.AddSingleton<VisualStyleController>();
            services.AddScoped<ClientHub>();
            services.AddScoped<LocalStorage>();
            services.AddScoped<ResolverCommunicator>();
            services.AddScoped<UserStateController>();
        }

        private static RedisEntityRepository<TType> GetRedisEntityRepository<TType>(IServiceProvider serviceProvider) where TType : class, IEntity<string>
        {
            var options = serviceProvider.GetService<IOptions<CommunicationOptions>>();
            if (options != null)
            {
                return new RedisEntityRepository<TType>(serviceProvider.GetService<ILogger<RedisEntityRepository<TType>>>(), options.Value.ConnectionString);
            }

            throw new InvalidOperationException($"Unable to resolve options class providing connection string for entity repository of type {typeof(TType)}.");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapHub<ClientHub>("/ClientHub");
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
