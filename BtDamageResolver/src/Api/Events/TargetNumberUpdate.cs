using System;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Api.Events
{
    /// <summary>
    /// Event describing a new set of target numbers for a specific weapon class and a specific unit.
    /// </summary>
    [Serializable]
    public class TargetNumberUpdate
    {
        public TargetNumberUpdate()
        {
            TimeStamp = DateTime.UtcNow;
        }

        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Target number calculation log shows how the target number was calculated.
        /// </summary>
        public AttackLog CalculationLog { get; set; }

        public int TargetNumber { get; set; }

        public Guid UnitId { get; set; }

        public Guid WeaponEntryId { get; set; }
    }
}