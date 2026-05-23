using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;

public class RepositoryProvider
{
    public IEntityRepository<Ammo, string> AmmoRepository { get; init; }
    public IEntityRepository<ArcDiagram, string> ArcDiagramRepository { get; init; }
    public IEntityRepository<ClusterTable, string> ClusterTableRepository { get; init; }
    public IEntityRepository<CriticalDamageTable, string> CriticalDamageTableRepository { get; init; }
    public IEntityRepository<PaperDoll, string> PaperDollRepository { get; init; }
    public IEntityRepository<Unit, string> UnitRepository { get; init; }
    public IEntityRepository<Weapon, string> WeaponRepository { get; init; }

    public RepositoryProvider(
        IEntityRepository<Ammo, string> ammoRepository,
        IEntityRepository<ArcDiagram, string> arcDiagramRepository,
        IEntityRepository<ClusterTable, string> clusterTableRepository,
        IEntityRepository<CriticalDamageTable, string> criticalDamageTableRepository,
        IEntityRepository<PaperDoll, string> paperDollRepository,
        IEntityRepository<Unit, string> unitRepository,
        IEntityRepository<Weapon, string> weaponRepository)
    {
        AmmoRepository = ammoRepository;
        ArcDiagramRepository = arcDiagramRepository;
        ClusterTableRepository = clusterTableRepository;
        CriticalDamageTableRepository = criticalDamageTableRepository;
        PaperDollRepository = paperDollRepository;
        UnitRepository = unitRepository;
        WeaponRepository = weaponRepository;
    }
}
