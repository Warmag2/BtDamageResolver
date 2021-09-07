using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class DamageReportList
    {
        public DamageReportList(DamageReport damageReport)
        {
            DamageReports = new List<DamageReport> {damageReport};
            Visible = true;
        }

        public void Add(DamageReport damageReport)
        {
            if (DamageReports.All(d => d.Id != damageReport.Id))
            {
                DamageReports.Add(damageReport);
            }
        }

        public void AddRange(List<DamageReport> damageReports)
        {
            foreach (var damageReport in damageReports)
            {
                DamageReports.Add(damageReport);
            }
        }

        public bool Remove(DamageReport damageReport)
        {
            return DamageReports.Remove(damageReport);
        }

        public bool Empty()
        {
            return !DamageReports.Any();
        }

        public List<DamageReport> DamageReports { get; set; }

        public bool Visible { get; set; }
    }
}