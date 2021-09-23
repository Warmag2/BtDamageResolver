using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Extensions
{
    public static class GrainFactoryExtensions
    {
        private const int RepositoryActorCommonId = 0;

        public static IClusterTableRepository GetClusterTableRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<IClusterTableRepository>(RepositoryActorCommonId);
        }

        public static IGameEntryRepository GetGameEntryRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<IGameEntryRepository>(RepositoryActorCommonId);
        }

        public static ICriticalDamageTableRepository GetCriticalDamageTableRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<ICriticalDamageTableRepository>(RepositoryActorCommonId);
        }

        public static IPaperDollRepository GetPaperDollRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<IPaperDollRepository>(RepositoryActorCommonId);
        }

        public static IUnitRepository GetUnitRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<IUnitRepository>(RepositoryActorCommonId);
        }

        public static IWeaponRepository GetWeaponRepository(this IGrainFactory grainFactory)
        {
            return grainFactory.GetGrain<IWeaponRepository>(RepositoryActorCommonId);
        }

    }
}