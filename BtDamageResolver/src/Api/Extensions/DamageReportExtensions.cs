using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class DamageReportExtensions
    {
        public static DamageReport Merge(this List<DamageReport> damageReports)
        {
            if (damageReports.Count == 0)
            {
                return null;
            }

            var first = damageReports.First();

            foreach (var report in damageReports.Skip(1))
            {
                first.Merge(report);
            }

            return first;
        }
    }
}