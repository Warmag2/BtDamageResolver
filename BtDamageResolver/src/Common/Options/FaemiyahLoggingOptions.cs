using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Common.Options;

/// <summary>
/// Configurable logging options in Orleans projects.
/// </summary>
public class FaemiyahLoggingOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FaemiyahLoggingOptions"/> class.
    /// </summary>
    public FaemiyahLoggingOptions()
    {
        LogLevel = LogLevel.Debug;
        LogLevelOrleans = LogLevel.Debug;
        LogToConsole = true;
        LogToDatabase = true;
        LoggingIntervalMilliseconds = 15000;
        SendDetailedErrorsToClient = false;
    }

    /// <summary>
    /// Gets or sets log level for program logic.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets log level for orleans infrastructure logic.
    /// </summary>
    public LogLevel LogLevelOrleans { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether log lines are appended to the console.
    /// </summary>
    public bool LogToConsole { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any data is logged to database.
    /// </summary>
    /// <remarks>
    /// Needed for the Grafana features to do anything. Disable if performance is poor or space is very limited.
    /// </remarks>
    public bool LogToDatabase { get; set; }

    /// <summary>
    /// Gets or sets the interval, in milliseconds, between database log-write loop iterations.
    /// </summary>
    /// <remarks>
    /// Controls how often queued game/player log entries are flushed to the database. Defaults to 15000 (four flushes per minute).
    /// </remarks>
    public int LoggingIntervalMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether exception details (message and stack trace)
    /// are sent back to the client on error.
    /// </summary>
    /// <remarks>
    /// Enable in development to surface errors immediately. Disable in production
    /// to avoid leaking internal implementation details to clients.
    /// </remarks>
    public bool SendDetailedErrorsToClient { get; set; }
}