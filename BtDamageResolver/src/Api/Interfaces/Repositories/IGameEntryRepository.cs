using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Interfaces.ActorInterfacePrototypes;
using Orleans;

namespace Faemiyah.BtDamageResolver.Api.Interfaces.Repositories
{
    /// <summary>
    /// Interface for a Game Entry Repository Actor.
    /// </summary>
    public interface IGameEntryRepository : IGrainWithIntegerKey, IEntityRepository<GameEntry, string>
    {
    }
}