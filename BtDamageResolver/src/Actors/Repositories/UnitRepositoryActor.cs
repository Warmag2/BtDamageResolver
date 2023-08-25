using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories;

/// <summary>
/// An Unit repository actor, which stores the properties of different units.
/// </summary>
public class UnitRepositoryActor : ExternalRepositoryActorBase<Unit, string>, IUnitRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitRepositoryActor"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="repository">The unit repository.</param>
    public UnitRepositoryActor(ILogger<UnitRepositoryActor> logger, CachedEntityRepository<Unit, string> repository) : base(logger, repository)
    {
    }
}