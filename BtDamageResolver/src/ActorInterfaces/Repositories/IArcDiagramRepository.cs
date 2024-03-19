using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for an Ammo Repository Actor.
/// </summary>
[Alias("IArcDiagramRepository")]
public interface IArcDiagramRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<ArcDiagram, string>
{
}