using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class WeaponFeatureEntry
    {
        /// <summary>
        /// Supplementary data needed for weapon feature resolution.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// The type of this weapon feature.
        /// </summary>
        public WeaponFeature Type { get; set; }

        public override string ToString()
        {
            var dataString = Data == null ? "null" : Data;
            return $"{Type} ({dataString})";
        }
    }
}