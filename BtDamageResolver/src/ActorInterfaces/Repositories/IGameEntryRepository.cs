using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for a Game Entry Repository Actor.
/// </summary>
[Alias("IGameEntryRepository")]
public interface IGameEntryRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<GameEntry, string>
{
}