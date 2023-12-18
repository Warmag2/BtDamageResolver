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
    /// Gets the ammo repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The ammo repository.</returns>
    public static IAmmoRepository GetAmmoRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IAmmoRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the arc diagram repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The arc diagram repository.</returns>
    public static IArcDiagramRepository GetArcDiagramRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IArcDiagramRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the cluster table repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The cluster table repository.</returns>
    public static IClusterTableRepository GetClusterTableRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IClusterTableRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the game entry repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The game entry repository.</returns>
    public static IGameEntryRepository GetGameEntryRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IGameEntryRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the critical damage table repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The critical damage table repository.</returns>
    public static ICriticalDamageTableRepository GetCriticalDamageTableRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<ICriticalDamageTableRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the paper doll repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The paper doll repository.</returns>
    public static IPaperDollRepository GetPaperDollRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IPaperDollRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the unit repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The unit repository.</returns>
    public static IUnitRepository GetUnitRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IUnitRepository>(RepositoryActorCommonId);
    }

    /// <summary>
    /// Gets the weapon repository.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The weapon repository.</returns>
    public static IWeaponRepository GetWeaponRepository(this IGrainFactory grainFactory)
    {
        return grainFactory.GetGrain<IWeaponRepository>(RepositoryActorCommonId);
    }
}