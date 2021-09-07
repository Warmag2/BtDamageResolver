using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class DamageReport
    {
        public DamageReport()
        {
            Id = Guid.NewGuid();
            AmmoUsageAttacker = new Dictionary<string, int>();
            AmmoUsageDefender = new Dictionary<string, int>();
            AttackLog = new AttackLog();
            TimeStamp = DateTime.UtcNow;
        }

        public Guid Id { get; set; }

        public Phase Phase { get; set; }

        public int InitialTroopers { get; set; }

        public Guid FiringUnitId { get; set; }

        public string FiringUnitName { get; set; }

        public Guid TargetUnitId { get; set; }

        public string TargetUnitName { get; set; }

        public Dictionary<string, int> AmmoUsageAttacker { get; set; }

        public Dictionary<string, int> AmmoUsageDefender { get; set; }

        public int AttackerHeat { get; set; }

        public int Turn { get; set; }

        public AttackLog AttackLog { get; set; }

        public DamagePaperDoll DamagePaperDoll { get; set; }

        public DateTime TimeStamp { get; set; }

        public void Log(AttackLogEntry entry)
        {
            AttackLog.Append(entry);
        }

        public void Merge(DamageReport damageReport)
        {
            if (FiringUnitId != damageReport.FiringUnitId || TargetUnitId != damageReport.TargetUnitId)
            {
                throw new InvalidOperationException("Firing and target units do not match. Trying to merge damage reports from different fire events.");
            }

            AttackLog.Append(damageReport.AttackLog);

            AttackerHeat += damageReport.AttackerHeat;

            foreach (var ammoUsageItem in damageReport.AmmoUsageAttacker)
            {
                SpendAmmoAttacker(ammoUsageItem.Key, ammoUsageItem.Value);
            }

            foreach (var ammoUsageItem in damageReport.AmmoUsageDefender)
            {
                SpendAmmoDefender(ammoUsageItem.Key, ammoUsageItem.Value);
            }

            DamagePaperDoll.Merge(damageReport.DamagePaperDoll);
        }

        public void SpendAmmoAttacker(string ammoType, int ammoAmount)
        {
            SpendAmmo(true, ammoType, ammoAmount);
        }

        public void SpendAmmoDefender(string ammoType, int ammoAmount)
        {
            SpendAmmo(false, ammoType, ammoAmount);
        }

        private void SpendAmmo(bool attacker, string ammoType, int ammoAmount)
        {
            var ammoDict = attacker ? AmmoUsageAttacker : AmmoUsageDefender;

            if (ammoDict.TryGetValue(ammoType, out var existingValue))
            {
                ammoDict[ammoType] = existingValue + ammoAmount;
            }
            else
            {
                ammoDict.Add(ammoType, ammoAmount);
            }
        }
    }
}
