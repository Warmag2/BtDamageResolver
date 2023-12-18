using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories;

/// <summary>
/// An Arc Diagram repository actor, which stores the arcs a weapon can shoot out from, per unit type.
/// </summary>
public class ArcDiagramRepositoryActor : ExternalRepositoryActorBase<ArcDiagram, string>, IArcDiagramRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArcDiagramRepositoryActor"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="repository">The ammo repository.</param>
    public ArcDiagramRepositoryActor(ILogger<ArcDiagramRepositoryActor> logger, CachedEntityRepository<ArcDiagram, string> repository) : base(logger, repository)
    {
    }
}