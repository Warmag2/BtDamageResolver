using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.Repositories;
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