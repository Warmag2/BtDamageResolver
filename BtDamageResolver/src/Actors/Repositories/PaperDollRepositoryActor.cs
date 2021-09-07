using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.Repositories;
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