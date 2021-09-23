using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A ClusterTable repository actor, which stores the properties of different ClusterTables.
    /// </summary>
    public class CriticalDamageTableRepositoryActor : ExternalRepositoryActorBase<CriticalDamageTable, string>, ICriticalDamageTableRepository
    {
        public CriticalDamageTableRepositoryActor(ILogger<CriticalDamageTableRepositoryActor> logger, CachedEntityRepository<CriticalDamageTable, string> repository) : base(logger, repository)
        {
        }
    }
}