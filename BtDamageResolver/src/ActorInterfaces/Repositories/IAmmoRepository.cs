using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories
{
    /// <summary>
    /// Interface for an Ammo Repository Actor.
    /// </summary>
    public interface IAmmoRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<Ammo, string>
    {
    }
}