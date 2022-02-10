using System;
using System.Collections.Concurrent;
using System.Threading;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Extensions.Logging;

using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Faemiyah.BtDamageResolver.Common.Logging
{
    /// <summary>
    /// Specific implementation of ILoggerFactory.
    /// Provides logging options into file system or ElasticSearch.
    /// Use extension method to enable logging in console / asp.net applications.
    /// See <see cref="FaemiyahLoggingExtensions"/>.
    /// </summary>
    public class FaemiyahLoggerFactory : ILoggerFactory, ILoggerProvider
    {
        private static readonly SemaphoreSlim LogCreationSemaphore = new(1, 1);
        private readonly ConcurrentDictionary<string, ILogger> _loggers;
        private readonly SerilogLoggerFactory _logFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaemiyahLoggerFactory"/> class.
        /// </summary>
        /// <param name="options">The logging options.</param>
        public FaemiyahLoggerFactory(IOptions<FaemiyahLoggingOptions> options)
        {
            _logFactory = new SerilogLoggerFactory(InitializeLogging(options.Value ?? new FaemiyahLoggingOptions()));
            _loggers = new ConcurrentDictionary<string, ILogger>();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider)
        {
            // Inherited method that does not need to do anything.
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Perform cleanup.
        /// </summary>
        /// <param name="disposing">Is the class disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            _loggers.Clear();
            _logFactory.Dispose();
        }
    }
}