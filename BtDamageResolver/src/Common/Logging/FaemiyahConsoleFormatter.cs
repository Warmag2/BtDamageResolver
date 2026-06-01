using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Faemiyah.BtDamageResolver.Common.Logging;

/// <summary>
/// A minimal single-line console formatter producing lines of the form
/// <c>{ISO-8601 UTC timestamp} - [{LogLevel}] - {Category} - {message}</c>,
/// with any exception appended on subsequent lines.
/// </summary>
public sealed class FaemiyahConsoleFormatter : ConsoleFormatter
{
    /// <summary>
    /// The name under which this formatter is registered.
    /// </summary>
    public const string FormatterName = "faemiyah";

    /// <summary>
    /// Initializes a new instance of the <see cref="FaemiyahConsoleFormatter"/> class.
    /// </summary>
    public FaemiyahConsoleFormatter() : base(FormatterName)
    {
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
        {
            return;
        }

        textWriter.Write(DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        textWriter.Write(" - [");
        textWriter.Write(GetLevelString(logEntry.LogLevel));
        textWriter.Write("] - ");
        textWriter.Write(logEntry.Category);
        textWriter.Write(" - ");
        textWriter.Write(message);

        if (logEntry.Exception is not null)
        {
            textWriter.Write(Environment.NewLine);
            textWriter.Write(logEntry.Exception.ToString());
        }

        textWriter.Write(Environment.NewLine);
    }

    private static string GetLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        _ => logLevel.ToString()
    };
}
