using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
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