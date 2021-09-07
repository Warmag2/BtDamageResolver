using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for an Unit Repository Actor.
    /// </summary>
    public interface IUnitRepository : IGrainWithIntegerKey, IEntityRepository<Unit, string>
    {
    }
}
