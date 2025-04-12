using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Tools.DataExporter;

/// <summary>
/// Data exporter main program class.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Program
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
        var configuration = GetConfiguration("DataExporterSettings.json");
        var section = configuration.GetSection(Settings.LoggingOptionsBlockName);
        var loggingOptions = Options.Create(section.Get<FaemiyahLoggingOptions>());
        var loggerFactory = new FaemiyahLoggerFactory(loggingOptions);

        var logger = loggerFactory.CreateLogger("DataExporter");
        logger.LogInformation(LogoText);
        logger.LogInformation(Disclaimer);

        var result = Parser.Default.ParseArguments<DataExportOptions>(args)
            .MapResult(
                initOptions => RunDataExport(logger, initOptions).Result,
                errs => 1);

        return result;
    }

    private static IConfiguration GetConfiguration(string settingsFile)
    {
        var config = new ConfigurationBuilder();
        return config.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(settingsFile).Build();
    }

    private static async Task<int> RunDataExport(ILogger logger, DataExportOptions options)
    {
        try
        {
            var dataExporter = new DataExporter(logger);
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
