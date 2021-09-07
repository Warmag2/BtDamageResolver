using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// A Weapon repository actor, which stores the properties of different Weapons.
    /// </summary>
    public class WeaponRepositoryActor : ExternalRepositoryActorBase<Weapon, string>, IWeaponRepository
    {
        public WeaponRepositoryActor(ILogger<WeaponRepositoryActor> logger, CachedEntityRepository<Weapon, string> repository) : base(logger, repository)
        {
        }
    }
}