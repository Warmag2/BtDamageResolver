using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// An Unit repository actor, which stores the properties of different units.
    /// </summary>
    public class UnitRepositoryActor : ExternalRepositoryActorBase<Unit, string>, IUnitRepository
    {
        public UnitRepositoryActor(ILogger<UnitRepositoryActor> logger, CachedEntityRepository<Unit, string> repository) : base(logger, repository)
        {
        }
    }
}