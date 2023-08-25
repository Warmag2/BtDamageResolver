using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer;

/// <summary>
/// The main program class.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main program entrypoint.
    /// </summary>
    /// <param name="args">Command line parameters.</param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// Build the host.
    /// </summary>
    /// <param name="args">Command line parameters.</param>
    /// <returns>The hostbuilder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
