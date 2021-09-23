using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A PaperDoll repository actor, which stores the properties of different PaperDolls.
    /// </summary>
    public class PaperDollRepositoryActor : ExternalRepositoryActorBase<PaperDoll, string>, IPaperDollRepository
    {
        public PaperDollRepositoryActor(ILogger<PaperDollRepositoryActor> logger, CachedEntityRepository<PaperDoll, string> repository) : base(logger, repository)
        {
        }
    }
}