using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for an Unit Repository Actor.
/// </summary>
[Alias("IUnitRepository")]
public interface IUnitRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<Unit, string>
{
}