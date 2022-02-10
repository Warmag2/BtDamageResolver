using System;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Enums;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Extensions
{
    /// <summary>
    /// Extensions for TimeSpans.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Gets the largest time unit in the given timespan.
        /// </summary>
        /// <param name="timeSpan">The timespan to check.</param>
        /// <returns>The largest time unit that fits into the given timespan.</returns>
        public static TimeUnit GetLargestTimeUnit(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return TimeUnit.Day;
            }

            if (timeSpan.TotalHours >= 1)
            {
                return TimeUnit.Hour;
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                return TimeUnit.Minute;
            }

            return TimeUnit.Second;
        }
    }
}