using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A ClusterTable repository actor, which stores the properties of different ClusterTables.
    /// </summary>
    public class CriticalDamageTableRepositoryActor : ExternalRepositoryActorBase<CriticalDamageTable, string>, ICriticalDamageTableRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CriticalDamageTableRepositoryActor"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="repository">The critical damage repository.</param>
        public CriticalDamageTableRepositoryActor(ILogger<CriticalDamageTableRepositoryActor> logger, CachedEntityRepository<CriticalDamageTable, string> repository) : base(logger, repository)
        {
        }
    }
}