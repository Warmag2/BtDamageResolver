using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for a Weapon Repository Actor.
    /// </summary>
    public interface IWeaponRepository : IGrainWithIntegerKey, IEntityRepository<Weapon, string>
    {
    }
}