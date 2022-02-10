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
    public class ClusterTableRepositoryActor : ExternalRepositoryActorBase<ClusterTable, string>, IClusterTableRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterTableRepositoryActor"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="repository">The cluster damage repository.</param>
        public ClusterTableRepositoryActor(ILogger<ClusterTableRepositoryActor> logger, CachedEntityRepository<ClusterTable, string> repository) : base(logger, repository)
        {
        }
    }
}