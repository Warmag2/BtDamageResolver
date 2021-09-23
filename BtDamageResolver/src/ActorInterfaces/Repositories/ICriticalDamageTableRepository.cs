using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for a CriticalDamageTable Repository Actor.
    /// </summary>
    public interface ICriticalDamageTableRepository : IGrainWithIntegerKey, IEntityRepository<CriticalDamageTable, string>
    {
    }
}