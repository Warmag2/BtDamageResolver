using System;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    /// <summary>
    /// An entity which contains a cluster table for 
    /// </summary>
    [Serializable]
    public class ClusterTable : NamedEntity
    {
        public int[][] Table { get; set; }

        public int GetDamage(int damageValue, int clusterRoll)
        {
            clusterRoll = Math.Clamp(clusterRoll, 2, 12);

            return Table[clusterRoll][damageValue];
        }

        public int GetDamage(int damageValue)
        {
            return Table[0][damageValue];
        }
    }
}