using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for a Weapon Repository Actor.
    /// </summary>
    public interface IWeaponRepository : IGrainWithIntegerKey, IEntityRepository<Weapon, string>
    {
    }
}