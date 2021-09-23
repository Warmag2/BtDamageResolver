using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for a ClusterTable Repository Actor.
    /// </summary>
    public interface IClusterTableRepository : IGrainWithIntegerKey, IEntityRepository<ClusterTable, string>
    {
    }
}