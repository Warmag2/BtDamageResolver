using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    /// <summary>
    /// Extensions for handling damage report lists.
    /// </summary>
    public static class DamageReportExtensions
    {
        /// <summary>
        /// Merge the damage reports in this damage report list.
        /// </summary>
        /// <param name="damageReports">The list of damage reports to merge.</param>
        /// <returns>A damage report consisting of the given list of damage reports.</returns>
        public static DamageReport Merge(this List<DamageReport> damageReports)
        {
            var damageReportsToProcess = damageReports.Where(d => d != null).ToList();

            if (damageReportsToProcess.Count == 0)
            {
                return null;
            }

            var first = damageReportsToProcess.First();

            foreach (var report in damageReportsToProcess.Skip(1))
            {
                first.Merge(report);
            }

            return first;
        }
    }
}