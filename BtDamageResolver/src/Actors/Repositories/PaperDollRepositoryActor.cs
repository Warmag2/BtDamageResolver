using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="PaperDollRepositoryActor"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="repository">The paper doll repository.</param>
        public PaperDollRepositoryActor(ILogger<PaperDollRepositoryActor> logger, CachedEntityRepository<PaperDoll, string> repository) : base(logger, repository)
        {
        }
    }
}