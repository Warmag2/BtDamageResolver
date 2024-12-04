using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Entities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;

/// <summary>
/// Contains methods which fetch game data and indicate whether data is valid for certain unit types.
/// </summary>
public class CommonData
{
    private const string BattleArmorWeaponPrefix = "BA ";
    private const string InfantryWeaponPrefix = "Infantry ";
    private const string MeleeWeaponPrefix = "Melee ";

    private static readonly int RangeShort = 6;
    private static readonly int[] RangesMedium = [6, 12];
    private static readonly int[] RangesLong = [6, 12, 20];
    private static readonly int[] RangesExtreme = [6, 12, 20, 25];

    private static readonly int RangeShortCapital = 12;
    private static readonly int[] RangesMediumCapital = [12, 24];
    private static readonly int[] RangesLongCapital = [12, 24, 40];
    private static readonly int[] RangesExtremeCapital = [12, 24, 40, 50];

    private readonly IEntityRepository<GameEntry, string> _gameEntryRepository;
    private readonly IEntityRepository<Unit, string> _unitRepository;
    private readonly SortedDictionary<string, string> _mapWeaponNamesNormal;
    private readonly SortedDictionary<string, string> _mapWeaponNamesBattleArmor;
    private readonly SortedDictionary<string, string> _mapWeaponNamesInfantry;
    private readonly SortedDictionary<string, string> _mapWeaponNamesMech;
    private readonly SortedDictionary<string, string> _mapWeaponNamesVehicle;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonData"/> class.
    /// </summary>
    /// <param name="arcRepository">The arc repository.</param>
    /// <param name="ammoRepository">The ammo repository.</param>
    /// <param name="clusterTableRepository">The cluster table repository.</param>
    /// <param name="criticalDamageTableRepository">The critical damage table repository.</param>
    /// <param name="gameEntryRepository">The game entry repository.</param>
    /// <param name="paperDollRepository">The paper doll repository.</param>
    /// <param name="unitRepository">The unit repository.</param>
    /// <param name="weaponRepository">The weapon repository.</param>
    public CommonData(
        IEntityRepository<ArcDiagram, string> arcRepository,
        IEntityRepository<Ammo, string> ammoRepository,
        IEntityRepository<ClusterTable, string> clusterTableRepository,
        IEntityRepository<CriticalDamageTable, string> criticalDamageTableRepository,
        IEntityRepository<GameEntry, string> gameEntryRepository,
        IEntityRepository<PaperDoll, string> paperDollRepository,
        IEntityRepository<Unit, string> unitRepository,
        IEntityRepository<Weapon, string> weaponRepository)
    {
        _gameEntryRepository = gameEntryRepository;
        _unitRepository = unitRepository;

        // Pre-bake lists used to generate options
        DictionaryUnitType = new List<UnitType>
        {
            UnitType.Building, UnitType.AerospaceCapital, UnitType.AerospaceDropshipAerodyne, UnitType.AerospaceDropshipSpheroid, UnitType.AerospaceFighter, UnitType.BattleArmor,
            UnitType.Infantry, UnitType.Mech, UnitType.VehicleTracked, UnitType.VehicleWheeled,
            UnitType.VehicleHover, UnitType.VehicleVtol
        }.ToDictionary(u => u.ToString());
        MapMovementAmount = new Dictionary<string, int>
        {
            { "0-2", 0 }, { "3-4", 3 }, { "5-6", 5 }, { "7-9", 7 }, { "10-17", 10 }, { "18-24", 18 }, { "25+", 25 }
        };
        MapAttackModifier = new Dictionary<string, int>
        {
            { "-4", -4 }, { "-3", -3 }, { "-2", -2 }, { "-1", -1 }, { "+0", 0 }, { "+1", 1 }, { "+2", 2 }, { "+3", 3 }, { "+4", 4 }
        };
        MapFacing = new Dictionary<string, Direction>
        {
            { "Front", Direction.Front }, { "Left", Direction.Left }, { "Right", Direction.Right }, { "Rear", Direction.Rear }, { "Up/Down", Direction.Top }
        };
        DictionaryArc = arcRepository.GetAll().OrderBy(a => a.UnitType).ToDictionary(a => a.UnitType);
        DictionaryAmmo = ammoRepository.GetAll().OrderBy(a => a.Name).ToDictionary(a => a.Name);
        DictionaryClusterTable = clusterTableRepository.GetAll().OrderBy(w => w.Name).ToDictionary(w => w.Name);
        DictionaryCriticalDamageTable = criticalDamageTableRepository.GetAll().OrderBy(w => w.GetId()).ToDictionary(w => w.GetId());
        DictionaryPaperDoll = paperDollRepository.GetAll().OrderBy(w => w.GetId()).ToDictionary(w => w.GetId());
        DictionaryFeature = new SortedDictionary<string, UnitFeature>(Enum.GetValues<UnitFeature>().ToDictionary(q => q.ToString()));
        DictionaryWeapon = weaponRepository.GetAll().OrderBy(w => w.Name).ToDictionary(w => w.Name);
        _mapWeaponNamesNormal = new SortedDictionary<string, string>(DictionaryWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix) && !w.StartsWith(MeleeWeaponPrefix)).ToDictionary(w => w));
        _mapWeaponNamesBattleArmor = new SortedDictionary<string, string>(DictionaryWeapon.Values.Select(w => w.Name).Where(w => w.StartsWith(BattleArmorWeaponPrefix)).ToDictionary(w => w.Substring(BattleArmorWeaponPrefix.Length), w => w));
        _mapWeaponNamesInfantry = new SortedDictionary<string, string>(DictionaryWeapon.Values.Select(w => w.Name).Where(w => w.StartsWith(InfantryWeaponPrefix)).ToDictionary(w => w.Substring(InfantryWeaponPrefix.Length), w => w));
        _mapWeaponNamesMech = new SortedDictionary<string, string>(DictionaryWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix)).ToDictionary(w => w));
        _mapWeaponNamesVehicle = new SortedDictionary<string, string>(DictionaryWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix) && !w.StartsWith(MeleeWeaponPrefix)).ToDictionary(w => w))
        {
            { "Melee Charge", "Melee Charge" }
        };
    }

    /// <summary>
    /// Dictionary for cluster tables.
    /// </summary>
    public Dictionary<string, ClusterTable> DictionaryClusterTable { get; }

    /// <summary>
    /// Dictionary for critical damage tables.
    /// </summary>
    public Dictionary<string, CriticalDamageTable> DictionaryCriticalDamageTable { get; }

    /// <summary>
    /// Dictionary for unit features.
    /// </summary>
    public SortedDictionary<string, UnitFeature> DictionaryFeature { get; set; }

    /// <summary>
    /// Dictionary for paperdolls.
    /// </summary>
    public Dictionary<string, PaperDoll> DictionaryPaperDoll { get; }

    /// <summary>
    /// Dictionary for unit types.
    /// </summary>
    public Dictionary<string, UnitType> DictionaryUnitType { get; }

    /// <summary>
    /// Dictionary for weapons.
    /// </summary>
    public Dictionary<string, Weapon> DictionaryWeapon { get; }

    /// <summary>
    /// Display map for attack modifier.
    /// </summary>
    public Dictionary<string, int> MapAttackModifier { get; }

    /// <summary>
    /// Display map for facing.
    /// </summary>
    public Dictionary<string, Direction> MapFacing { get; }

    /// <summary>
    /// Display map for movement amounts.
    /// </summary>
    public Dictionary<string, int> MapMovementAmount { get; }

    /// <summary>
    /// Dictionary for ammo types.
    /// </summary>
    private Dictionary<string, Ammo> DictionaryAmmo { get; }

    /// <summary>
    /// Dictionary for arcs.
    /// </summary>
    private Dictionary<UnitType, ArcDiagram> DictionaryArc { get; }

    /// <summary>
    /// Constructs a blank unit.
    /// </summary>
    /// <returns>A blank unit.</returns>
    public static UnitEntry GetBlankUnit()
    {
        return new()
        {
            Type = UnitType.Mech,
            Name = "New Unit",
            Gunnery = 4,
            Piloting = 5,
            Sinks = 20,
            Speed = 4,
            Tonnage = 50
        };
    }

    /// <summary>
    /// Gets a default weapon for an unit type.
    /// </summary>
    /// <param name="unitType">The type of the unit to ask for.</param>
    /// <returns>The default weapon for the given unit type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit type is unknown.</exception>
    public static WeaponEntry GetDefaultWeapon(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Building:
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
            case UnitType.AerospaceFighter:
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
            case UnitType.VehicleHover:
            case UnitType.VehicleTracked:
            case UnitType.VehicleVtol:
            case UnitType.VehicleWheeled:
                return new WeaponEntry
                {
                    TimeStamp = DateTime.UtcNow,
                    State = WeaponState.Active,
                    WeaponName = "Medium Laser"
                };
            case UnitType.BattleArmor:
                return new WeaponEntry
                {
                    TimeStamp = DateTime.UtcNow,
                    State = WeaponState.Active,
                    WeaponName = "BA Machine Gun"
                };
            case UnitType.Infantry:
                return new WeaponEntry
                {
                    TimeStamp = DateTime.UtcNow,
                    State = WeaponState.Active,
                    WeaponName = "Infantry Rifle Ballistic"
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(unitType), unitType, null);
        }
    }

    /// <summary>
    /// Gets a default weapon bay for an unit type.
    /// </summary>
    /// <param name="unitType">The type of the unit to ask for.</param>
    /// <returns>The default weapon bay for the given unit type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit type is unknown.</exception>
    public static WeaponBay GetDefaultWeaponBay(UnitType unitType)
    {
        return new WeaponBay
        {
            FiringSolution = new FiringSolution(),
            Name = "Default",
            Weapons = new List<WeaponEntry> { GetDefaultWeapon(unitType) }
        };
    }

    /// <summary>
    /// Creates a display map for cover options.
    /// </summary>
    /// <param name="unitType">The unit type to create for.</param>
    /// <returns>A display map for cover options for the given unit type.</returns>
    public static Dictionary<string, Cover> FormMapCover(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                return new Dictionary<string, Cover> { { "None", Cover.None }, { "Lower", Cover.Lower }, { "Upper", Cover.Upper }, { "Left", Cover.Left }, { "Right", Cover.Right } };
            default:
                return new Dictionary<string, Cover> { { "None", Cover.None } };
        }
    }

    /// <summary>
    /// Creates a map of valid movement classes for the given unit.
    /// </summary>
    /// <param name="unit">The unit to create the map for.</param>
    /// <returns>A map of valid movement classes for the given unit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the unit type is unknown.</exception>
    public static Dictionary<string, MovementClass> FormMapMovementClass(Unit unit)
    {
        switch (unit.Type)
        {
            case UnitType.Building:
                return GenerateOptions(new List<MovementClass> { MovementClass.Immobile });
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
            case UnitType.AerospaceFighter:
                return GenerateOptions(new List<MovementClass> { MovementClass.Immobile, MovementClass.Normal, MovementClass.Fast, MovementClass.OutOfControl });
            case UnitType.BattleArmor:
            case UnitType.Infantry:
                var listOfModesInfantry = new List<MovementClass> { MovementClass.Normal };
                if (unit.JumpJets > 0)
                {
                    listOfModesInfantry.Add(MovementClass.Jump);
                }

                return GenerateOptions(listOfModesInfantry);
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                var listOfModesMech = new List<MovementClass> { MovementClass.Immobile, MovementClass.Stationary, MovementClass.Normal, MovementClass.Fast };
                if (unit.HasFeature(UnitFeature.Masc))
                {
                    listOfModesMech.Add(MovementClass.Masc);
                }

                if (unit.JumpJets > 0)
                {
                    listOfModesMech.Add(MovementClass.Jump);
                }

                return GenerateOptions(listOfModesMech);
            case UnitType.VehicleHover:
            case UnitType.VehicleTracked:
            case UnitType.VehicleVtol:
            case UnitType.VehicleWheeled:
                var listOfModesVehicle = new List<MovementClass> { MovementClass.Immobile, MovementClass.Stationary, MovementClass.Normal, MovementClass.Fast };
                if (unit.HasFeature(UnitFeature.Masc))
                {
                    listOfModesVehicle.Add(MovementClass.Masc);
                }

                return GenerateOptions(listOfModesVehicle);
            default:
                throw new ArgumentOutOfRangeException(nameof(unit), unit.Type, null);
        }
    }

    /// <summary>
    /// Creates a map of valid stances for the given unit type.
    /// </summary>
    /// <param name="type">The unit type to create the map for.</param>
    /// <returns>A map of valid stances for the given unit.</returns>
    public static Dictionary<string, Stance> FormMapStance(UnitType type)
    {
        switch (type)
        {
            case UnitType.BattleArmor:
                return new Dictionary<string, Stance> { { "None", Stance.Normal }, { "Light", Stance.Light }, { "Hardened", Stance.Hardened }, { "Heavy", Stance.Heavy } };
            case UnitType.Infantry:
                return new Dictionary<string, Stance> { { "None", Stance.Normal }, { "DugIn", Stance.DugIn }, { "Prone", Stance.Prone }, { "Light", Stance.Light }, { "Hardened", Stance.Hardened }, { "Heavy", Stance.Heavy } };
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                return new Dictionary<string, Stance> { { "Normal", Stance.Normal }, { "Crouch", Stance.Crouch }, { "Prone", Stance.Prone } };
            default:
                return new Dictionary<string, Stance> { { "Normal", Stance.Normal } };
        }
    }

    /// <summary>
    /// Form pick brackets for valid unit speeds.
    /// </summary>
    /// <returns>Pick brackets for selecting valid speeds.</returns>
    public static List<PickBracket> FormPickBracketsSpeed()
    {
        return MakeSimplePickBrackets(1, 1, 20);
    }

    /// <summary>
    /// Form pick brackets for valid numbers of jump jets.
    /// </summary>
    /// <returns>Pick brackets for selecting valid numbers of jump jets.</returns>
    public static List<PickBracket> FormPickBracketsJumpJets()
    {
        return MakeSimplePickBrackets(0, 1, 12);
    }

    /// <summary>
    /// Form pick brackets for valid firing penalty amounts.
    /// </summary>
    /// <returns>Pick brackets for selecting valid firing penalty amounts.</returns>
    public static List<PickBracket> FormPickBracketsPenalty()
    {
        return MakeSimplePickBrackets(0, 1, 12);
    }

    /// <summary>
    /// Form pick brackets for valid skill values.
    /// </summary>
    /// <returns>Pick brackets for selecting valid skill values.</returns>
    public static List<PickBracket> FormPickBracketsSkills()
    {
        return MakeSimplePickBrackets(1, 1, 8);
    }

    /// <summary>
    /// Form pick brackets for selecting unit tonnage.
    /// </summary>
    /// <returns>Pick brackets for selecting unit tonnage.</returns>
    public static List<PickBracket> FormPickBracketsTonnage()
    {
        return MakeSimplePickBrackets(0, 5, 100);
    }

    /// <summary>
    /// Form pick brackets for selecting the number of troopers.
    /// </summary>
    /// <returns>Pick brackets for selecting the number of troopers.</returns>
    public static List<PickBracket> FormPickBracketsTroopers()
    {
        return MakeSimplePickBrackets(1, 1, 30);
    }

    /// <summary>
    /// Form pick brackets for selecting the number of heat sinks.
    /// </summary>
    /// <returns>Pick brackets for selecting the number of heat sinks.</returns>
    public static List<PickBracket> FormPickBracketsSinks()
    {
        return MakeSimplePickBrackets(0, 1, 60);
    }

    /// <summary>
    /// Form pick brackets for valid numbers of weapons.
    /// </summary>
    /// <returns>Pick brackets for selecting valid numbers of weapons.</returns>
    public static List<PickBracket> FormPickBracketsWeaponAmount()
    {
        return MakeSimplePickBrackets(1, 1, 12);
    }

    /// <summary>
    /// Saves an unit into the repository.
    /// </summary>
    /// <param name="unit">The unit to save.</param>
    /// <returns>A task which finishes when the unit has saved.</returns>
    public async Task SaveUnit(UnitEntry unit)
    {
        await _unitRepository.AddOrUpdateAsync(unit.ToUnit());
    }

    /// <summary>
    /// Gets the list of names for all saved units.
    /// </summary>
    /// <returns>The list of all unit names.</returns>
    public SortedDictionary<string, string> GetSavedUnitNames()
    {
        var sortedUnitList = new SortedDictionary<string, string>();
        _unitRepository.GetAllKeys().ForEach(u => sortedUnitList.Add(u, u));
        return sortedUnitList;
    }

    /// <summary>
    /// Gets all game entries.
    /// </summary>
    /// <returns>A list of all ongoing games.</returns>
    public List<GameEntry> GetGameEntries()
    {
        return _gameEntryRepository.GetAll();
    }

    /// <summary>
    /// Gets an unit from the unit repository.
    /// </summary>
    /// <param name="unitName">The name of the unit to get.</param>
    /// <returns>The unit, if found, and null otherwise.</returns>
    public async Task<Unit> GetUnit(string unitName)
    {
        return await _unitRepository.GetAsync(unitName);
    }

    /// <summary>
    /// Gets the default ammo for a weapon.
    /// </summary>
    /// <param name="weaponName">The name of the weapon.</param>
    /// <returns>The default ammo for the given weapon, or null, if none found.</returns>
    public string GetWeaponDefaultAmmo(string weaponName)
    {
        if (DictionaryWeapon.TryGetValue(weaponName, out var weapon))
        {
            return weapon.AmmoDefault;
        }

        return null;
    }

    /// <summary>
    /// Does the weapon have any ammo options.
    /// </summary>
    /// <param name="weaponName">The weapon name to check.</param>
    /// <returns><b>True</b> if the weapon has ammo options, <b>false</b> otherwise.</returns>
    public bool WeaponHasAmmo(string weaponName)
    {
        if (DictionaryWeapon.TryGetValue(weaponName, out var weapon))
        {
            return weapon.Ammo.Count != 0;
        }

        return false;
    }

    /// <summary>
    /// Deletes an unit from the repository.
    /// </summary>
    /// <param name="unitName">The unit name to delete.</param>
    /// <returns><b>True</b> if the unit was deleted, <b>false</b> otherwise.</returns>
    public async Task<bool> DeleteUnit(string unitName)
    {
        return await _unitRepository.DeleteAsync(unitName);
    }

    /// <summary>
    /// Creates a map of arc valid for a given unit type.
    /// </summary>
    /// <param name="unitType">The unit type to create for.</param>
    /// <returns>A display map for arc options for the given unit type.</returns>
    public Dictionary<string, Arc> FormMapArc(UnitType unitType)
    {
        return DictionaryArc[unitType].Arcs.ToDictionary(a => a.ToString(), a => a);
    }

    /// <summary>
    /// Create a map of valid movement amounts for a given unit.
    /// </summary>
    /// <param name="unitEntry">The unit to create the map for.</param>
    /// <returns>The map for valid movement amount options for the given unit.</returns>
    public Dictionary<string, int> FormMapMovementAmount(UnitEntry unitEntry)
    {
        if (unitEntry.MovementClass == MovementClass.Jump)
        {
            return MakeSimplePickBrackets(0, 1, unitEntry.JumpJets).ToDictionary(p => p.ToString(), p => p.Begin);
        }

        var maxMovementAmount = unitEntry.GetCurrentSpeed();

        var possibleMovementAmounts = MapMovementAmount.Where(k => k.Value != 0 && k.Value <= maxMovementAmount).Select(k => k.Value).ToList();

        possibleMovementAmounts.AddRange(possibleMovementAmounts.Select(a => a - 1).ToList());
        possibleMovementAmounts.Add(0);

        possibleMovementAmounts.Add(maxMovementAmount);

        var brackets = MakeDualSidedPickBrackets(possibleMovementAmounts);

        return brackets.ToDictionary(p => p.ToString(), p => p.Begin);
    }

    /// <summary>
    /// Creates a display map of the ammo types for a given weapon.
    /// </summary>
    /// <param name="weaponName">The weapon to form the ammo map for.</param>
    /// <returns>A map of weapon ammo types.</returns>
    public SortedDictionary<string, string> FormMapWeaponAmmo(string weaponName)
    {
        return new SortedDictionary<string, string>(DictionaryWeapon[weaponName].Ammo.Keys.ToDictionary(k => k));
    }

    /// <summary>
    /// Create a map of valid weapon names for the given unit type.
    /// </summary>
    /// <param name="type">The unit type to crate for.</param>
    /// <returns>A map of valid weapon names for the given unit type.</returns>
    public SortedDictionary<string, string> FormMapWeaponName(UnitType type)
    {
        switch (type)
        {
            case UnitType.BattleArmor:
                return _mapWeaponNamesBattleArmor;
            case UnitType.Infantry:
                return _mapWeaponNamesInfantry;
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                return _mapWeaponNamesMech;
            case UnitType.VehicleHover:
            case UnitType.VehicleTracked:
            case UnitType.VehicleVtol:
            case UnitType.VehicleWheeled:
                return _mapWeaponNamesVehicle;
            default:
                return _mapWeaponNamesNormal;
        }
    }

    /// <summary>
    /// Form pick brackets for distance options when firing weapons.
    /// </summary>
    /// <param name="weaponBay">The unit to form the pick brackets for.</param>
    /// <param name="type">The firing unit type.</param>
    /// <returns>Pick brackets for selecting valid engagement distances.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range bracket to form for is unknown.</exception>
    public List<PickBracket> FormPickBracketsDistance(WeaponBay weaponBay, UnitType type)
    {
        var allRangeChanges = new List<int>();

        foreach (var weaponEntry in weaponBay.Weapons)
        {
            var weapon = FormWeapon(weaponEntry);
            switch (type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropshipAerodyne:
                case UnitType.AerospaceDropshipSpheroid:
                case UnitType.AerospaceFighter:
                    switch (weapon.RangeAerospace)
                    {
                        case RangeBracket.Short:
                            allRangeChanges.Add(weapon.CapitalScale ? RangeShortCapital : RangeShort);
                            break;
                        case RangeBracket.Medium:
                            allRangeChanges.AddRange(weapon.CapitalScale ? RangesMediumCapital : RangesMedium);
                            break;
                        case RangeBracket.Long:
                            allRangeChanges.AddRange(weapon.CapitalScale ? RangesLongCapital : RangesLong);
                            break;
                        case RangeBracket.Extreme:
                            allRangeChanges.AddRange(weapon.CapitalScale ? RangesExtremeCapital : RangesExtreme);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(weaponBay), "Invalid range bracket.");
                    }

                    break;
                default:
                    allRangeChanges.AddRange(weapon.Range.Values);
                    if (weapon.RangeMinimum != -1)
                    {
                        for (int ii = 0; ii <= weapon.RangeMinimum; ii++)
                        {
                            allRangeChanges.Add(ii);
                        }
                    }

                    break;
            }
        }

        return MakeArbitraryPickBrackets(allRangeChanges);
    }

    /// <summary>
    /// Forms a weapon from a weapon entry.
    /// </summary>
    /// <param name="weaponEntry">The weapon entry.</param>
    /// <returns>The combined weapon with the ammo applied.</returns>
    public Weapon FormWeapon(WeaponEntry weaponEntry)
    {
        return FormWeapon(weaponEntry.WeaponName, weaponEntry.Ammo);
    }

    /// <summary>
    /// Forms a weapon from ammo and weapon entities.
    /// </summary>
    /// <param name="weaponName">The weapon name.</param>
    /// <param name="ammoName">The name of the ammo to apply.</param>
    /// <returns>The combined weapon with the ammo applied.</returns>
    public Weapon FormWeapon(string weaponName, string ammoName)
    {
        var weapon = DictionaryWeapon[weaponName];

        if (!string.IsNullOrWhiteSpace(ammoName) && weapon.Ammo.TryGetValue(ammoName, out var value))
        {
            var ammo = DictionaryAmmo[value];
            return weapon.ApplyAmmo(ammo);
        }

        return weapon;
    }

    private static Dictionary<string, TEnumType> GenerateOptions<TEnumType>(List<TEnumType> validOptions = null)
        where TEnumType : Enum
    {
        return validOptions == null ?
            Enum.GetValues(typeof(TEnumType)).Cast<TEnumType>().ToDictionary(e => e.ToString()) :
            validOptions.ToDictionary(e => e.ToString());
    }

    private static List<PickBracket> MakeSimplePickBrackets(int begin, int interval, int end)
    {
        var pickBrackets = new List<PickBracket>();

        for (int ii = begin; ii <= end; ii += interval)
        {
            pickBrackets.Add(new PickBracket
            {
                Begin = ii,
                End = ii
            });
        }

        return pickBrackets;
    }

    private static List<PickBracket> MakeArbitraryPickBrackets(List<int> allChangeLocations)
    {
        var pickBrackets = new List<PickBracket>();

        allChangeLocations = allChangeLocations.Distinct().ToList();
        allChangeLocations.Sort();

        pickBrackets.Add(new PickBracket { Begin = 0, End = allChangeLocations[0] });

        for (int ii = 1; ii < allChangeLocations.Count; ii++)
        {
            pickBrackets.Add(new PickBracket { Begin = allChangeLocations[ii - 1] + 1, End = allChangeLocations[ii] });
        }

        return pickBrackets;
    }

    private static List<PickBracket> MakeDualSidedPickBrackets(List<int> allChangeLocations)
    {
        if (allChangeLocations.Count == 1 && allChangeLocations[0] == 0)
        {
            return new List<PickBracket>
            {
                new()
                {
                    Begin = 0,
                    End = 0
                }
            };
        }

        allChangeLocations = allChangeLocations.Distinct().ToList();
        allChangeLocations.Sort();

        var pickBrackets = new List<PickBracket>();

        for (int ii = 1; ii < allChangeLocations.Count; ii += 2)
        {
            pickBrackets.Add(new PickBracket { Begin = allChangeLocations[ii - 1], End = allChangeLocations[ii] });
        }

        if (allChangeLocations.Count % 2 != 0)
        {
            pickBrackets.Add(new PickBracket { Begin = allChangeLocations[^1], End = allChangeLocations[^1] });
        }

        return pickBrackets;
    }
}