using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
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
        public ClusterTableRepositoryActor(ILogger<ClusterTableRepositoryActor> logger, CachedEntityRepository<ClusterTable, string> repository) : base(logger, repository)
        {
        }
    }
}