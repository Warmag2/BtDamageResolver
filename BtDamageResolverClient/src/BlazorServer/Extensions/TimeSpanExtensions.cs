using System;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Enums;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Extensions
{
    public static class TimeSpanExtensions
    {
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