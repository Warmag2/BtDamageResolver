using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class DamageReportExtensions
    {
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