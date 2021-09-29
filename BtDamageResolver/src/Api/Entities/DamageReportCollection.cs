using System;
using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// Helper class that contains a set of damage reports and the turns that they happened on.
    /// Also contains manipulation methods, which keep track of the timestamps of the damage reports.
    /// </summary>
    public class DamageReportCollection
    {
        public DamageReportCollection()
        {
            TimeStamp = DateTime.UtcNow;
            DamageReports = new SortedDictionary<int, List<DamageReport>>();
            Visibility = new SortedDictionary<int, bool>();
        }

        /// <summary>
        /// The damage reports themselves
        /// </summary>
        public SortedDictionary<int, List<DamageReport>> DamageReports { get; set; }

        /// <summary>
        /// The last update time of this damage report collection
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Visibility of individual damage report turns.
        /// </summary>
        public SortedDictionary<int, bool> Visibility { get; set; }

        public bool Add(DamageReport damageReport)
        {
            if (!DamageReports.ContainsKey(damageReport.Turn))
            {
                DamageReports.Add(damageReport.Turn, new List<DamageReport>());
                Visibility.Add(damageReport.Turn, true);
            }

            if (DamageReports[damageReport.Turn].All(d => d.Id != damageReport.Id))
            {
                DamageReports[damageReport.Turn].Add(damageReport);
                TimeStamp = DateTime.UtcNow;
                
                return true;
            }

            return false;
        }

        public bool AddRange(List<DamageReport> damageReports)
        {
            return damageReports.Aggregate(false, (current, damageReport) => current | Add(damageReport));
        }

        public void Clear()
        {
            DamageReports.Clear();
            Visibility.Clear();

            TimeStamp = DateTime.UtcNow;
        }

        public List<DamageReport> GetAll()
        {
            return DamageReports.SelectMany(d => d.Value).ToList();
        }

        public bool Remove(DamageReport damageReport)
        {
            var damageReportToRemove = DamageReports[damageReport.Turn].SingleOrDefault(d => d.Id == damageReport.Id);

            if (DamageReports[damageReport.Turn].Remove(damageReportToRemove))
            {
                if (!DamageReports[damageReport.Turn].Any())
                {
                    DamageReports.Remove(damageReport.Turn);
                    Visibility.Remove(damageReport.Turn);
                }

                TimeStamp = DateTime.UtcNow;
                
                return true;
            }

            return false;
        }

        public bool Remove(int turn)
        {
            var changes= Visibility.Remove(turn) && DamageReports.Remove(turn);
            
            if (changes)
            {
                TimeStamp = DateTime.UtcNow;
            }
            
            return changes;
        }

        public bool Visible(int turn)
        {
            if (DamageReports.ContainsKey(turn))
            {
                return Visibility[turn];
            }

            return false;
        }

        public void ToggleVisible(int turn)
        {
            if (DamageReports.ContainsKey(turn))
            {
                Visibility[turn] = !Visibility[turn];
            }

            TimeStamp = DateTime.UtcNow;
        }

        public bool IsEmpty()
        {
            return !DamageReports.Any();
        }
    }
}