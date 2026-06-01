using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Faemiyah.BtDamageResolver.Common.Logging;

/// <summary>
/// Specific implementation of <see cref="ILoggerFactory"/> backed by the built-in
/// Microsoft.Extensions.Logging.Console provider, using
/// <see cref="FaemiyahConsoleFormatter"/> for output formatting.
/// Use the <see cref="FaemiyahLoggingExtensions"/> extension method to enable logging in console / asp.net applications.
/// </summary>
public sealed class FaemiyahLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _innerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaemiyahLoggerFactory"/> class.
    /// </summary>
    /// <param name="options">The logging options.</param>
    public FaemiyahLoggerFactory(IOptions<FaemiyahLoggingOptions> options)
    {
        var loggingOptions = options.Value ?? new FaemiyahLoggingOptions();
        _innerFactory = LoggerFactory.Create(builder => ConfigureBuilder(builder, loggingOptions));
    }

    /// <summary>
    /// Configures a logging builder with the Faemiyah console logging setup.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <param name="options">The logging options.</param>
    public static void ConfigureBuilder(ILoggingBuilder builder, FaemiyahLoggingOptions options)
    {
        builder.ClearProviders();
        builder.SetMinimumLevel(options.LogLevel);

        // Orleans infrastructure logging is filtered to its own level, matching the previous Serilog behaviour.
        builder.AddFilter("Orleans", options.LogLevelOrleans);

        if (options.LogToConsole)
        {
            builder.AddConsoleFormatter<FaemiyahConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddConsole(consoleOptions => consoleOptions.FormatterName = FaemiyahConsoleFormatter.FormatterName);
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => _innerFactory.CreateLogger(categoryName);

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider) => _innerFactory.AddProvider(provider);

    /// <inheritdoc />
    public void Dispose() => _innerFactory.Dispose();
}
