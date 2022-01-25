using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Client.BlazorServer.Entities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class CommonData
    {
        private readonly IEntityRepository<GameEntry, string> _gameEntryRepository;
        private readonly IEntityRepository<Unit, string> _unitRepository;
        private readonly SortedDictionary<string, string> _mapWeaponNamesNormal;
        private readonly SortedDictionary<string, string> _mapWeaponNamesBattleArmor;
        private readonly SortedDictionary<string, string> _mapWeaponNamesInfantry;
        private readonly SortedDictionary<string, string> _mapWeaponNamesMech;
        private readonly SortedDictionary<string, string> _mapWeaponNamesVehicle;

        private const string BattleArmorWeaponPrefix = "BA ";
        private const string InfantryWeaponPrefix = "Infantry ";
        private const string MeleeWeaponPrefix = "Melee ";

        public CommonData(
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
            MapUnitType = new List<UnitType>
            {
                UnitType.Building, UnitType.AerospaceDropship, UnitType.AerospaceFighter, UnitType.BattleArmor,
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
            /*MapCover = new Dictionary<string, Cover>
            {
                { "None", Cover.None }, { "Lower", Cover.Lower }, { "Upper", Cover.Upper }, { "Left", Cover.Left }, { "Right", Cover.Right }, { "Light", Cover.Light }, { "Hardened", Cover.Hardened }, { "Heavy", Cover.Heavy }
            };*/
            MapFacing = new Dictionary<string, Direction>
            {
                { "Front", Direction.Front }, { "Left", Direction.Left }, { "Right", Direction.Right }, { "Rear", Direction.Rear }, { "Up/Down", Direction.Top }
            };
            MapClusterTable = clusterTableRepository.GetAllAsync().Result.OrderBy(w => w.Name).ToDictionary(w => w.Name);
            MapCriticalDamageTable = criticalDamageTableRepository.GetAllAsync().Result.OrderBy(w => w.GetId()).ToDictionary(w => w.GetId());
            MapPaperDoll = paperDollRepository.GetAllAsync().Result.OrderBy(w => w.GetId()).ToDictionary(w => w.GetId());
            MapFeature = new SortedDictionary<string, UnitFeature>(Enum.GetValues<UnitFeature>().ToDictionary(q => q.ToString()));
            MapWeapon = weaponRepository.GetAllAsync().Result.OrderBy(w => w.Name).ToDictionary(w => w.Name);
            _mapWeaponNamesNormal = new SortedDictionary<string, string>(MapWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix) && !w.StartsWith(MeleeWeaponPrefix)).ToDictionary(w => w));
            _mapWeaponNamesBattleArmor = new SortedDictionary<string, string>(MapWeapon.Values.Select(w => w.Name).Where(w => w.StartsWith(BattleArmorWeaponPrefix)).ToDictionary(w => w.Substring(BattleArmorWeaponPrefix.Length), w => w));
            _mapWeaponNamesInfantry = new SortedDictionary<string, string>(MapWeapon.Values.Select(w => w.Name).Where(w => w.StartsWith(InfantryWeaponPrefix)).ToDictionary(w => w.Substring(InfantryWeaponPrefix.Length), w => w));
            _mapWeaponNamesMech = new SortedDictionary<string, string>(MapWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix)).ToDictionary(w => w));
            _mapWeaponNamesVehicle = new SortedDictionary<string, string>(MapWeapon.Values.Select(w => w.Name).Where(w => !w.StartsWith(BattleArmorWeaponPrefix) && !w.StartsWith(InfantryWeaponPrefix) && !w.StartsWith(MeleeWeaponPrefix)).ToDictionary(w => w));
            _mapWeaponNamesVehicle.Add("Melee Charge", "Melee Charge");
        }

        public Dictionary<string, UnitType> MapUnitType { get; }
        
        public Dictionary<string, int> MapAttackModifier { get; }

        public Dictionary<string, ClusterTable> MapClusterTable { get; }

        //public Dictionary<string, Cover> MapCover { get; }

        public Dictionary<string, CriticalDamageTable> MapCriticalDamageTable { get; }

        public Dictionary<string, Direction> MapFacing { get; }
        
        public SortedDictionary<string, UnitFeature> MapFeature { get; set; }

        public Dictionary<string, int> MapMovementAmount { get; }

        public Dictionary<string, PaperDoll> MapPaperDoll { get; }

        public Dictionary<string, Weapon> MapWeapon { get; }

        public Dictionary<string, Cover> CreateMapCover(UnitType type)
        {
            switch (type)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return new Dictionary<string, Cover> { { "None", Cover.None }, { "Lower", Cover.Lower }, { "Upper", Cover.Upper }, { "Left", Cover.Left }, { "Right", Cover.Right } };
                default:
                    return new Dictionary<string, Cover> { { "None", Cover.None } };
            }
        }

        public SortedDictionary<string, string> CreateMapWeaponName(UnitType type)
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

        public Dictionary<string, MovementClass> CreateMapMovementClass(Unit unit)
        {
            switch (unit.Type)
            {
                case UnitType.Building:
                    return GenerateOptions(new List<MovementClass> { MovementClass.Immobile });
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
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

        public Dictionary<string, Stance> CreateMapStance(UnitType type)
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

        private Dictionary<string, TEnumType> GenerateOptions<TEnumType>(List<TEnumType> validOptions = null) where TEnumType : Enum
        {
            return validOptions == null ?
                Enum.GetValues(typeof(TEnumType)).Cast<TEnumType>().ToDictionary(e => e.ToString()) :
                validOptions.ToDictionary(e => e.ToString());
            //return validOptions == null ? Enum.GetValues(typeof(TEnumType)).Cast<object>().Select(o => o.ToString()).ToList() : validOptions.Select(e => e.ToString()).ToList();
        }
        
        public static WeaponEntry GetDefaultWeapon(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Building:
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
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
                        Mode = WeaponMode.Normal,
                        State = WeaponState.Active,
                        WeaponName = "Medium Laser"
                    };
                case UnitType.BattleArmor:
                    return new WeaponEntry
                    {
                        TimeStamp = DateTime.UtcNow,
                        Mode = WeaponMode.Normal,
                        State = WeaponState.Active,
                        WeaponName = "BA Machine Gun"
                    };
                case UnitType.Infantry:
                    return new WeaponEntry
                    {
                        TimeStamp = DateTime.UtcNow,
                        Mode = WeaponMode.Normal,
                        State = WeaponState.Active,
                        WeaponName = "Infantry Rifle Ballistic"
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitType), unitType, null);
            }
        }

        public static UnitEntry GetBlankUnit()
        {
            return new()
            {
                Type = UnitType.Mech,
                Name = "New Unit"
            };
        }

        public async Task SaveUnit(UnitEntry unit)
        {
            await _unitRepository.AddOrUpdateAsync(unit.ToUnit());
        }

        public SortedDictionary<string, string> GetSavedUnits()
        {
            var sortedUnitList = new SortedDictionary<string, string>();
            _unitRepository.GetAllKeys().ForEach(u => sortedUnitList.Add(u, u));
            return sortedUnitList;
        }

        public List<GameEntry> GetGameEntries()
        {
            return _gameEntryRepository.GetAll();
        }

        public async Task<Unit> GetUnitEntry(string unitName)
        {
            return await _unitRepository.GetAsync(unitName);
        }

        public async Task DeleteUnit(string unitName)
        {
            await _unitRepository.DeleteAsync(unitName);
        }

        public List<PickBracket> FormPickBracketsDistance(UnitEntry unit)
        {
            var allRangeChanges = new List<int>();

            foreach (var weaponEntry in unit.Weapons)
            {
                var weapon = MapWeapon[weaponEntry.WeaponName];
                if (unit.Type == UnitType.AerospaceDropship || unit.Type == UnitType.AerospaceFighter)
                {
                    switch (weapon.RangeAerospace)
                    {
                        case RangeBracket.Short:
                            allRangeChanges.Add(6);
                            break;
                        case RangeBracket.Medium:
                            allRangeChanges.AddRange(new[] { 6, 12 });
                            break;
                        case RangeBracket.Long:
                            allRangeChanges.AddRange(new[] { 6, 12, 20 });
                            break;
                        case RangeBracket.Extreme:
                            allRangeChanges.AddRange(new[] { 6, 12, 20, 25 });
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    allRangeChanges.AddRange(weapon.Range.Values);
                    if (weapon.RangeMinimum[weaponEntry.Mode] != -1)
                    {
                        for (int ii = 0; ii <= weapon.RangeMinimum[weaponEntry.Mode]; ii++)
                        {
                            allRangeChanges.Add(ii);
                        }
                    }
                }
            }

            return MakeArbitraryPickBrackets(allRangeChanges);
        }

        public List<PickBracket> FormPickBracketsSpeed()
        {
            return MakeSimplePickBrackets(1, 1, 20);
        }

        public List<PickBracket> FormPickBracketsJumpJets()
        {
            return MakeSimplePickBrackets(0, 1, 12);
        }

        public List<PickBracket> FormPickBracketsPenalty()
        {
            return MakeSimplePickBrackets(0, 1, 12);
        }

        public List<PickBracket> FormPickBracketsSkills()
        {
            return MakeSimplePickBrackets(1, 1, 8);
        }

        public List<PickBracket> FormPickBracketsTonnage()
        {
            return MakeSimplePickBrackets(0, 5, 100);
        }

        public List<PickBracket> FormPickBracketsTroopers()
        {
            return MakeSimplePickBrackets(1, 1, 30);
        }

        public List<PickBracket> FormPickBracketsSinks()
        {
            return MakeSimplePickBrackets(0, 1, 60);
        }

        public Dictionary<string, int> CreateMapMovementAmount(UnitEntry unitEntry)
        {
            if (unitEntry.MovementClass == MovementClass.Jump)
            {
                return MakeSimplePickBrackets(0, 1, unitEntry.JumpJets).ToDictionary(p => p.ToString(), p => p.Begin);
            }

            var maxMovementAmount = unitEntry.GetCurrentSpeed(unitEntry.MovementClass);

            var possibleMovementAmounts = MapMovementAmount.Where(k => k.Value != 0 && k.Value <= maxMovementAmount).Select(k => k.Value).ToList();

            possibleMovementAmounts.AddRange(possibleMovementAmounts.Select(a => a - 1).ToList());
            possibleMovementAmounts.Add(0);

            possibleMovementAmounts.Add(maxMovementAmount);
            
            var brackets = MakeDualSidedPickBrackets(possibleMovementAmounts);

            return brackets.ToDictionary(p => p.ToString(), p => p.Begin);
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

            for (int ii = 1; ii < allChangeLocations.Count; ii+=2)
            {
                pickBrackets.Add(new PickBracket { Begin = allChangeLocations[ii-1], End = allChangeLocations[ii] });
            }

            if (allChangeLocations.Count % 2 != 0)
            {
                pickBrackets.Add(new PickBracket { Begin = allChangeLocations.Last(), End = allChangeLocations.Last() });
            }

            return pickBrackets;
        }
    }
}