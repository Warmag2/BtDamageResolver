using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommandLine;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Logging.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter;

/// <summary>
/// Data importer main program class.
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
        var configuration = Host.CreateApplicationBuilder(args).Configuration;
        var section = configuration.GetSection(Settings.LoggingOptionsBlockName);
        var loggingOptions = Options.Create(section.Get<FaemiyahLoggingOptions>());
        var loggerFactory = new FaemiyahLoggerFactory(loggingOptions);

        var logger = loggerFactory.CreateLogger("DataImporter");
        logger.LogInformation(LogoText);
        logger.LogInformation(Disclaimer);

        var result = Parser.Default.ParseArguments<DataImportOptions>(args)
            .MapResult(
                initOptions => RunDataImport(loggerFactory, configuration, initOptions).Result,
                errs => 1);

        return result;
    }

    private static async Task<int> RunDataImport(ILoggerFactory loggerFactory, IConfiguration configuration, DataImportOptions options)
    {
        try
        {
            var dataImporter = new DataImporter(loggerFactory, configuration);
            await dataImporter.Work(options);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }

        return 0;
    }
}
