using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// A special damage entry.
    /// </summary>
    [Serializable]
    public class SpecialDamageEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialDamageEntry"/> class.
        /// </summary>
        public SpecialDamageEntry()
        {
            Data = "0";
            Type = SpecialDamageType.None;
        }

        /// <summary>
        /// The freeform data for this special damage entry.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// The type of this special damage entry.
        /// </summary>
        public SpecialDamageType Type { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type} ({Data})";
        }

        /// <summary>
        /// Null this special damage entry.
        /// </summary>
        public void Clear()
        {
            Type = SpecialDamageType.None;
            Data = "0";
        }
    }
}