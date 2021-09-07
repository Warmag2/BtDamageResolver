using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for a PaperDoll Repository Actor.
    /// </summary>
    public interface IPaperDollRepository : IGrainWithIntegerKey, IEntityRepository<PaperDoll, string>
    {
    }
}