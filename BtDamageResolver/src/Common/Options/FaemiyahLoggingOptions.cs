using System.Reflection;
using Serilog.Events;

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
        LogLevel = LogEventLevel.Debug;
        LogLevelOrleans = LogEventLevel.Debug;
        LogToConsole = true;
        LogToDatabase = true;
        LogToFile = false;
        ProgramName = Assembly.GetExecutingAssembly().GetName().Name;
        LogFile = $"/logs/{ProgramName}.log";
    }

    /// <summary>
    /// Gets or sets the name of the logfile to log to.
    /// </summary>
    public string LogFile { get; set; }

    /// <summary>
    /// Gets or sets log level for program logic.
    /// </summary>
    public LogEventLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets log level for orleans infrastructure logic.
    /// </summary>
    public LogEventLevel LogLevelOrleans { get; set; }

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
    /// Gets or sets a value indicating whether log lines are appended to a file.
    /// </summary>
    public bool LogToFile { get; set;  }

    /// <summary>
    /// Gets or sets name of the executing program.
    /// </summary>
    /// <remarks>
    /// This property will be enriched onto each log line.
    /// </remarks>
    public string ProgramName { get; set; }
}