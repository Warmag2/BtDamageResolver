using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Logical unit partial class which deals with damage packet formation.
    /// </summary>
    public partial class LogicUnit
    {
        protected List<DamagePacket> Clusterize(int damage, int weaponClusterSize, int weaponClusterDamage, SpecialDamageEntry specialDamageEntry, bool onlyApplySpecialDamageOnce = true)
        {
            var damagePackets = new List<DamagePacket>();
            var first = true;

            while (damage > 0)
            {
                var currentClusterSize = Math.Clamp(damage, 1, weaponClusterSize);

                // Typically we only the first cluster hit applies the special damage entry, if any, so clustering does not multiply any special damage
                var clusterSpecialDamageEntry = first && onlyApplySpecialDamageOnce
                    ? new List<SpecialDamageEntry> {
                        new SpecialDamageEntry
                        {
                            Data = LogicHelper.MathExpression.Parse(specialDamageEntry.Data).ToString(),
                            Type = specialDamageEntry.Type
                        }
                    }
                    : new List<SpecialDamageEntry>
                    {
                        new SpecialDamageEntry()
                    };

                damagePackets.Add(new DamagePacket(currentClusterSize * weaponClusterDamage, clusterSpecialDamageEntry));
                damage -= currentClusterSize;

                first = false;
            }

            return damagePackets;
        }
    }
}
