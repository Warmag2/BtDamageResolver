using System;
using System.Collections.Generic;

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
        /// Projected ammo, estimate.
        /// </summary>
        public Dictionary<string, double> AmmoEstimate { get; set; }

        /// <summary>
        /// Projected ammo, worst-case.
        /// </summary>
        public Dictionary<string, int> AmmoWorstCase { get; set; }

        /// <summary>
        /// Projected heat, estimate.
        /// </summary>
        public double HeatEstimate { get; set; }

        /// <summary>
        /// Projected heat, worst-case.
        /// </summary>
        public int HeatWorstCase { get; set; }

        /// <summary>
        /// The target numbers.
        /// </summary>
        public Dictionary<Guid, TargetNumberUpdateSingleWeapon> TargetNumbers { get; set; }

        /// <summary>
        /// The unit ID for this calculated target number.
        /// </summary>
        public Guid UnitId { get; set; }
    }
}