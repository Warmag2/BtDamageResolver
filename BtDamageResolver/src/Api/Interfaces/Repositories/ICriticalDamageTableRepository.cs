using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for a CriticalDamageTable Repository Actor.
    /// </summary>
    public interface ICriticalDamageTableRepository : IGrainWithIntegerKey, IEntityRepository<CriticalDamageTable, string>
    {
    }
}