using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for a Game Entry Repository Actor.
    /// </summary>
    public interface IGameEntryRepository : IGrainWithIntegerKey, IEntityRepository<GameEntry, string>
    {
    }
}