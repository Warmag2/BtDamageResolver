using System.Reflection;
using Serilog.Events;

namespace Faemiyah.BtDamageResolver.Common.Options
{
    /// <summary>
    /// Configurable logging options in Orleans projects.
    /// </summary>
    public class FaemiyahLoggingOptions
    {
        /// <summary>
        /// The name of the logfile to log to.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Log level for program logic.
        /// </summary>
        public LogEventLevel LogLevel { get; set; }

        /// <summary>
        /// Log level for orleans infrastructure logic.
        /// </summary>
        public LogEventLevel LogLevelOrleans { get; set; }

        /// <summary>
        /// Append log lines to console
        /// </summary>
        public bool LogToConsole { get; set; }

        /// <summary>
        /// Are logs put into local file system
        /// </summary>
        public bool LogToDatabase { get; set; }

        /// <summary>
        /// Are logs put into local file system
        /// </summary>
        public bool LogToFile { get; set;  }

        /// <summary>
        /// Name of the executing program / asp.net project
        /// </summary>
        public string ProgramName { get; set; }

        public FaemiyahLoggingOptions()
        {
            LogLevel = LogEventLevel.Debug;
            LogLevelOrleans = LogEventLevel.Debug;
            LogToConsole = true;
            LogToDatabase = false;
            LogToFile = false;
            ProgramName = Assembly.GetExecutingAssembly().GetName().Name;
            LogFile = $"/log/{ProgramName}.log";
        }
    }
}
