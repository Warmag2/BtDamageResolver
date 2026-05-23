using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;

/// <summary>
/// Extension methods for grain fetching.
/// </summary>
public static class GrainFactoryExtensions
{
    private const long RepositoryActorCommonId = 0;

    /// <summary>
    /// Gets the game entry repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The game entry repository.</returns>
    public static IGameEntryRepository GetGameEntryRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IGameEntryRepository>(RepositoryActorCommonId);
    }
}