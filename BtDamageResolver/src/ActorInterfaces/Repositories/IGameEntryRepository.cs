using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for a Game Entry Repository Actor.
    /// </summary>
    public interface IGameEntryRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<GameEntry, string>
    {
    }
}