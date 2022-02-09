using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    /// <summary>
    /// The paper doll.
    /// </summary>
    [Serializable]
    public class PaperDoll : EntityBase<string>
    {
        /// <summary>
        /// The type of the paper doll.
        /// </summary>
        public PaperDollType Type { get; set; }

        /// <summary>
        /// The type of the attack.
        /// </summary>
        public AttackType AttackType { get; set; }

        /// <summary>
        /// The direction of the attack.
        /// </summary>
        public Direction Direction { get; set; }

        /// <summary>
        /// List of optional rules that apply for this paperdoll.
        /// </summary>
        public List<Rule> Rules { get; set; }

        /// <summary>
        /// The location mapping of this paper doll. 2d6 -> location.
        /// </summary>
        public Dictionary<int, List<Location>> LocationMapping { get; set; }

        /// <summary>
        /// The critical damage mapping of this paper doll. 2d6 -> damage.
        /// </summary>
        public Dictionary<int, CriticalDamageTableType> CriticalDamageMapping { get; set; }

        /// <summary>
        /// Gets the ID of the correct paper doll based on the given properties.
        /// </summary>
        /// <param name="paperDollType">The paper doll type.</param>
        /// <param name="attackType">The attack type.</param>
        /// <param name="direction">The attack direction.</param>
        /// <param name="rules">The game rules.</param>
        /// <returns>The ID of the paper doll to use.</returns>
        public static string GetIdFromProperties(PaperDollType paperDollType, AttackType attackType, Direction direction, List<Rule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return $"{paperDollType}_{attackType}_{direction}";
            }

            // Ordered list of relevant rules as part of the ID.
            return $"{paperDollType}_{attackType}_{direction}_{string.Join('_', rules.Select(r => r.ToString()).OrderBy(r => r))}";
        }

        /// <summary>
        /// Gets a new damage paper doll based on this paper doll.
        /// </summary>
        /// <returns>A damage paper doll based on this paper doll.</returns>
        public DamagePaperDoll GetDamagePaperDoll()
        {
            return new DamagePaperDoll(this);
        }

        /// <summary>
        /// Needed because the id is concatenated from several properties in this paperdoll.
        /// </summary>
        /// <returns>The ID of this paper doll.</returns>
        public override string GetId()
        {
            return GetIdFromProperties(Type, AttackType, Direction, Rules);
        }

        /// <inheritdoc />
        public override void SetId(string id)
        {
            throw new InvalidOperationException("You should never have to set a PaperDoll Id manually.");
        }
    }
}
