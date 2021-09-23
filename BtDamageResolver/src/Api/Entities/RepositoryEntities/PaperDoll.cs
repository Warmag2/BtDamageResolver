using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    [Serializable]
    public class PaperDoll : EntityBase<string>
    {
        public PaperDollType Type { get; set; }

        public AttackType AttackType { get; set; }

        public Direction Direction { get; set; }

        /// <summary>
        /// List of optional rules that apply for this paperdoll.
        /// </summary>
        public List<Rule> Rules { get; set; }

        public Dictionary<int, List<Location>> LocationMapping { get; set; }

        public Dictionary<int, CriticalDamageTableType> CriticalDamageMapping { get; set; }

        public DamagePaperDoll GetDamagePaperDoll()
        {
            return new DamagePaperDoll(this);
        }

        /// <summary>
        /// Needed because the id is concatenated from several properties in this paperdoll.
        /// </summary>
        /// <returns></returns>
        public override string GetId()
        {
            return GetIdFromProperties(Type, AttackType, Direction, Rules);
        }

        public static string GetIdFromProperties(PaperDollType paperDollType, AttackType attackType, Direction direction, List<Rule> rules)
        {
            if (rules == null || rules.Count==0)
            {
                return $"{paperDollType}_{attackType}_{direction}";
            }

            // Ordered list of relevant rules as part of the ID.
            return $"{paperDollType}_{attackType}_{direction}_{string.Join('_', rules.Select(r => r.ToString()).OrderBy(r => r))}";
        }

        public override void SetId(string id)
        {
            throw new InvalidOperationException("You should never have to set a PaperDoll Id manually.");
        }
    }
}
