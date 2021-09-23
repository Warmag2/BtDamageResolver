using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for an Unit Repository Actor.
    /// </summary>
    public interface IUnitRepository : IGrainWithIntegerKey, IEntityRepository<Unit, string>
    {
    }
}
