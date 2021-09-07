using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for a ClusterTable Repository Actor.
    /// </summary>
    public interface IClusterTableRepository : IGrainWithIntegerKey, IEntityRepository<ClusterTable, string>
    {
    }
}