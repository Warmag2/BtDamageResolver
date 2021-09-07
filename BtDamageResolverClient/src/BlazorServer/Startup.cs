using System;
using System.Threading.Tasks;
using Blazored.LocalStorage;
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
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;
using ConnectionOptions = Orleans.Configuration.ConnectionOptions;

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
            var clusterOptions = configuration.GetSection("ClusterOptions").Get<FaemiyahClusterOptions>();
            var client = ConnectClient(clusterOptions);

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
                options.MaximumReceiveMessageSize = 262144;
            });
            services.AddSingleton<CommonData>();
            services.AddSingleton<VisualStyleController>();
            services.AddSingleton(client);
            services.AddScoped<UserStateController>();
            services.AddScoped<ClientHub>();
            services.AddScoped<IResolverCommunicator, ResolverCommunicator>();
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

        private static IClusterClient ConnectClient(FaemiyahClusterOptions clusterOptions)
        {
            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "faemiyah";
                    options.ServiceId = "Resolver";
                })
                .Configure<ConnectionOptions>(options =>
                {
                    options.ConnectionRetryDelay = TimeSpan.FromSeconds(30);
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(30);
                })
                .Configure<MessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromMinutes(15);
                })
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = clusterOptions.Invariant;
                    options.ConnectionString = clusterOptions.ConnectionString;
                })
                .Build();

            client.Connect(ex => Task.FromResult(true)).Wait();
            return client;
        }
    }
}
