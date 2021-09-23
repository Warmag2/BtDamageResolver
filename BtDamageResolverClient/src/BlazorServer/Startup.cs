using System;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Communicators;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Communication;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Hubs;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            services.Configure<FaemiyahClusterOptions>(configuration.GetSection("ClusterOptions"));
            services.Configure<FaemiyahLoggingOptions>(configuration.GetSection("LoggingOptions"));
            services.Configure<CircuitOptions>(options =>
            {
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromDays(1);
            });
            services.Configure<HttpConnectionDispatcherOptions>(options =>
            {
                options.ApplicationMaxBufferSize = 262144;
                options.TransportMaxBufferSize = 262144;
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
            services.AddSingleton<IEntityRepository<ClusterTable, string>, SqlEntityRepository<ClusterTable>>();
            services.AddSingleton<IEntityRepository<CriticalDamageTable, string>, SqlEntityRepository<CriticalDamageTable>>();
            services.AddSingleton<IEntityRepository<GameEntry, string>, SqlEntityRepository<GameEntry>>();
            services.AddSingleton<IEntityRepository<PaperDoll, string>, SqlEntityRepository<PaperDoll>>();
            services.AddSingleton<IEntityRepository<Unit, string>, SqlEntityRepository<Unit>>();
            services.AddSingleton<IEntityRepository<Weapon, string>, SqlEntityRepository<Weapon>>();
            services.AddSingleton<CachedEntityRepository<ClusterTable, string>>();
            services.AddSingleton<CachedEntityRepository<CriticalDamageTable, string>>();
            services.AddSingleton<CachedEntityRepository<GameEntry, string>>();
            services.AddSingleton<CachedEntityRepository<PaperDoll, string>>();
            services.AddSingleton<CachedEntityRepository<Unit, string>>();
            services.AddSingleton<CachedEntityRepository<Weapon, string>>();
            services.AddSingleton<CommonData>();
            services.AddSingleton<VisualStyleController>();
            services.AddScoped<ClientHub>();
            services.AddScoped<LocalStorage>();
            services.AddScoped<ResolverCommunicator>();
            services.AddScoped<UserStateController>();
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
