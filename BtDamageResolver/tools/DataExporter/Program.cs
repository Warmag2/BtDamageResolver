using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommandLine;
using Faemiyah.Common.Hosting.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Tools.DataExporter;

/// <summary>
/// Data exporter main program class.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Program
{
    private const string LogoText = @"Faemiyah";

    private const string Disclaimer = "This application is intended only for internal data entry and testing.";

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Zero on no errors, nonzero on error.</returns>
    public static int Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddFaemiyahLogging();
        using var host = builder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        var logger = loggerFactory.CreateLogger("DataExporter");
        logger.LogInformation(LogoText);
        logger.LogInformation(Disclaimer);

        var result = Parser.Default.ParseArguments<DataExportOptions>(args)
            .MapResult(
                initOptions => RunDataExport(loggerFactory, configuration, initOptions).Result,
                errs => 1);

        return result;
    }

    private static async Task<int> RunDataExport(ILoggerFactory loggerFactory, IConfiguration configuration, DataExportOptions options)
    {
        try
        {
            var dataExporter = new DataExporter(loggerFactory, configuration);
            await dataExporter.Work(options);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }

        return 0;
    }
}
