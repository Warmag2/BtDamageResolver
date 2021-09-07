using System;
using System.Collections.Concurrent;
using System.Threading;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Common.Logging
{
    /// <summary>
    /// Thor specific implementation of ILoggerFactory.
    /// Provides logging options into file system or ElasticSearch.
    /// Use extension method to enable logging in console / asp.net applications.
    /// <see cref="FaemiyahLoggingExtensions"/>
    /// </summary>
    public class FaemiyahLoggerFactory : ILoggerFactory, ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers;
        private readonly SerilogLoggerFactory _logFactory;
        private static readonly SemaphoreSlim LogCreationSemaphore = new SemaphoreSlim(1, 1);

        public FaemiyahLoggerFactory(IOptions<FaemiyahLoggingOptions> options)
        {
            _logFactory = new SerilogLoggerFactory(InitializeLogging(options.Value ?? new FaemiyahLoggingOptions()));
            _loggers = new ConcurrentDictionary<string, ILogger>();
        }

        public ILogger CreateLogger(string categoryName)
        {
            LogCreationSemaphore.Wait();
            try
            {

                if (_loggers.TryGetValue(categoryName, out var storedLogger))
                {
                    return storedLogger;
                }

                var createdLogger = _logFactory.CreateLogger(categoryName);

                if (!_loggers.TryAdd(categoryName, createdLogger))
                {
                    throw new InvalidOperationException("Could not add the new logger to the dictionary. This should never happen.");
                }

                return createdLogger;
            }
            finally
            {
                LogCreationSemaphore.Release();
            }
        }

        /// <summary>
        /// Logs an information entry about a setting value.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="settingKey">The setting key.</param>
        /// <param name="settingValue">The setting value.</param>
        public static void LogInfoSettings(ILogger logger, string settingKey, object settingValue)
        {
            logger.LogInformation("Settings :: {0}={1}", settingKey, settingValue);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
            _loggers.Clear();
            _logFactory.Dispose();
        }
    }
}