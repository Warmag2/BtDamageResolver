using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// An Ammo repository actor, which stores the properties of different Ammo.
    /// </summary>
    public class AmmoRepositoryActor : ExternalRepositoryActorBase<Ammo, string>, IAmmoRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmmoRepositoryActor"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="repository">The ammo repository.</param>
        public AmmoRepositoryActor(ILogger<AmmoRepositoryActor> logger, CachedEntityRepository<Ammo, string> repository) : base(logger, repository)
        {
        }
    }
}