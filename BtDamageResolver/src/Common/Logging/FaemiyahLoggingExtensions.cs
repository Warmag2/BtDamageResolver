using System;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Common.Logging
{
    /// <summary>
    /// Extension class which allows enabling logging with ILoggingBuilder.
    /// In ASP.NET applications add logging with following code:
    ///
    /// .ConfigureLogging((hostingContext, logging) =>
    /// {
    ///     logging.AddFaemiyahLogging();
    /// })
    ///
    /// In console applications use ServiceCollection.
    /// </summary>
    public static class FaemiyahLoggingExtensions
    {
        /// <summary>
        /// Enables Thor system logging features (Currently ELK stack)
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddFaemiyahLogging(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, FaemiyahLoggerFactory>());
            return builder;
        }

        /// <summary>
        /// Register FaemiyahLoggerFactory
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FaemiyahLoggingOptions"/>.</param>
        public static ILoggingBuilder AddFaemiyahLogging(this ILoggingBuilder builder, Action<FaemiyahLoggingOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddFaemiyahLogging();
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
