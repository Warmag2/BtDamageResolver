using System;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// Event class defining a new set of target numbers for a specific weapon class and a specific unit.
    /// </summary>
    [Serializable]
    public class TargetNumberUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetNumberUpdate"/> class.
        /// </summary>
        public TargetNumberUpdate()
        {
            TimeStamp = DateTime.UtcNow;
        }

        /// <summary>
        /// The update timestamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Target number calculation log shows how the target number was calculated.
        /// </summary>
        public AttackLog CalculationLog { get; set; }

        /// <summary>
        /// The target number.
        /// </summary>
        public int TargetNumber { get; set; }

        /// <summary>
        /// The unit ID for this calculated target number.
        /// </summary>
        public Guid UnitId { get; set; }

        /// <summary>
        /// The weapon entry ID for this calculated target number.
        /// </summary>
        public Guid WeaponEntryId { get; set; }
    }
}