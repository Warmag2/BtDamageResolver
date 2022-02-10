using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A Weapon repository actor, which stores the properties of different Weapons.
    /// </summary>
    public class WeaponRepositoryActor : ExternalRepositoryActorBase<Weapon, string>, IWeaponRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponRepositoryActor"/> class.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        /// <param name="repository">The weapon repository.</param>
        public WeaponRepositoryActor(ILogger<WeaponRepositoryActor> logger, CachedEntityRepository<Weapon, string> repository) : base(logger, repository)
        {
        }
    }
}