using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Api;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Interfaces.Extensions;
using Orleans;

using static Faemiyah.BtDamageResolver.Actors.Logic.LogicCombatHelpers;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public class LogicHits : ILogicHits
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IMathExpression _mathExpression;
        private readonly IResolverRandom _random;

        public LogicHits(IGrainFactory grainFactory, IMathExpression mathExpression, IResolverRandom random)
        {
            _grainFactory = grainFactory;
            _mathExpression = mathExpression;
            _random = random;
        }

        public async Task ResolveHits(DamageReport damageReport, Dictionary<Rule, bool> rules, FiringSolution firingSolution, int marginOfSuccess, UnitEntry targetUnit, Weapon weapon, WeaponMode mode, List<(int damage, List<SpecialDamageEntry> specialDamageEntries)> damagePackets)
        {
            foreach (var damagePacket in damagePackets)
            {
                var (location, criticalDamageTableType) = GetLocation(damageReport, firingSolution.Cover, damageReport.DamagePaperDoll.PaperDoll, targetUnit);

                if (HitIsBlockedByCover(firingSolution.Cover, location, targetUnit))
                {
                    damageReport.Log(new AttackLogEntry { Context = $"Hit to {location} blocked by cover", Type = AttackLogEntryType.Information});

                    continue;
                }

                // Unfortunately we still have to do transformations at this point, as certain locations and armor types receive damage differently
                var transformedDamage = TransformDamageAmountBasedOnArmor(damageReport, location, targetUnit, damagePacket.damage);
                transformedDamage = TransformDamageAmountBasedOnLocation(damageReport, location, targetUnit, transformedDamage);
                
                damageReport.DamagePaperDoll.RecordDamage(location, transformedDamage);
                damageReport.Log(new AttackLogEntry { Location = location, Number = transformedDamage, Type = AttackLogEntryType.Damage });

                foreach (var specialDamageEntry in damagePacket.specialDamageEntries)
                {
                    if (specialDamageEntry.Type != SpecialDamageType.None)
                    {
                        switch (specialDamageEntry.Type)
                        {
                            case SpecialDamageType.Critical:
                            case SpecialDamageType.Motive:
                                damageReport.Log(new AttackLogEntry { Context = "Weapon special damage induces a critical damage threat", Type = AttackLogEntryType.Information });

                                var criticalTableType = specialDamageEntry.Type == SpecialDamageType.Critical ? CriticalDamageTableType.Critical : CriticalDamageTableType.Motive;

                                var criticalDamageTableId = GetCriticalDamageTableName(targetUnit.Type, criticalTableType, location);
                                var criticalDamageTable = await _grainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);

                                // Critical rolls from damage may have a modifier.
                                var specialDamageThreatModifier = _mathExpression.Parse(specialDamageEntry.Data);
                                damageReport.Log(new AttackLogEntry { Context = "Threat roll modifier from damage source", Number = specialDamageThreatModifier, Type = AttackLogEntryType.Calculation });
                                var glancingBlowModifier = IsGlancingBlow(marginOfSuccess, targetUnit) ? -2 : 0;
                                if (glancingBlowModifier != 0)
                                {
                                    damageReport.Log(new AttackLogEntry { Context = "Threat roll glancing blow modifier", Number = glancingBlowModifier, Type = AttackLogEntryType.Calculation});
                                }

                                var specialDamageEntryCriticalThreatRoll = _random.D26() + specialDamageThreatModifier + glancingBlowModifier;
                                damageReport.Log(new AttackLogEntry { Context = "Threat roll", Number = specialDamageEntryCriticalThreatRoll, Type = AttackLogEntryType.DiceRoll });

                                if (criticalDamageTable.Mapping[specialDamageEntryCriticalThreatRoll].Any(c => c != CriticalDamageType.None))
                                {
                                    // Critical damage threats coming from special damage (AP Ammo, Retractable Blade) never do multiple critical effects. Take the first.
                                    // Direct critical damage threats should always succeed, regardless of damage threshold. Give a very high damage amount.
                                    var criticalDamageType = criticalDamageTable.Mapping[specialDamageEntryCriticalThreatRoll].First();
                                    damageReport.DamagePaperDoll.RecordCriticalDamage(location, damagePacket.damage, CriticalThreatType.Normal, criticalDamageType);
                                    damageReport.Log(new AttackLogEntry { Context = criticalDamageType.ToString(), Number = 0, Location = location, Type = AttackLogEntryType.Critical });
                                }

                                break;
                            default:
                                damageReport.DamagePaperDoll.RecordSpecialDamage(location, specialDamageEntry);
                                damageReport.Log(new AttackLogEntry { Context = specialDamageEntry.Type.ToString(), Location = location, Number = int.Parse(specialDamageEntry.Data), Type = AttackLogEntryType.SpecialDamage });
                                break;
                        }
                    }
                }

                if (criticalDamageTableType != CriticalDamageTableType.None)
                {
                    var criticalThreatRoll = _random.D26();

                    damageReport.Log(new AttackLogEntry
                    {
                        Context = "Critical threat",
                        Number = criticalThreatRoll,
                        Type = AttackLogEntryType.DiceRoll
                    });

                    await ResolveCriticalHit(damageReport, targetUnit, location, criticalThreatRoll, damagePacket.damage, transformedDamage, criticalDamageTableType);
                }
            }
        }

        private async Task ResolveCriticalHit(DamageReport damageReport, UnitEntry targetUnit, Location location, int criticalThreatRoll, int inducingDamage, int locationTransformedDamage, CriticalDamageTableType criticalDamageTableType)
        {
            var criticalDamageTableId = GetCriticalDamageTableName(targetUnit.Type, criticalDamageTableType, location);
            var criticalDamageTable = await _grainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);

            switch (targetUnit.Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropship:
                case UnitType.AerospaceFighter:
                    if (criticalThreatRoll > 7)
                    {
                        var aerospaceCriticalHitRoll = _random.D26();
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = "Aerospace critical hit roll", Number = aerospaceCriticalHitRoll,
                            Type = AttackLogEntryType.DiceRoll
                        });

                        damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.DamageThreshold, criticalDamageTable.Mapping[aerospaceCriticalHitRoll]);
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = string.Join(", ", criticalDamageTable.Mapping[aerospaceCriticalHitRoll].Select(c => c.ToString())),
                            Number = locationTransformedDamage,
                            Location = location,
                            Type = AttackLogEntryType.Critical
                        });
                    }

                    break;
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    break;
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    // Simulate arms and legs being able to be blown off
                    if (criticalThreatRoll == 12 &&
                        (location == Location.LeftArm || location == Location.LeftLeg ||
                         location == Location.RightArm || location == Location.RightLeg))
                    {
                        damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.Normal, CriticalDamageType.BlownOff);
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = string.Join(", ", criticalDamageTable.Mapping[criticalThreatRoll].Select(c => c.ToString())),
                            Number = locationTransformedDamage,
                            Location = location,
                            Type = AttackLogEntryType.Critical
                        });
                    }
                    else if (criticalDamageTable.Mapping[criticalThreatRoll].Any(c => c != CriticalDamageType.None))
                    {
                        damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.Normal, criticalDamageTable.Mapping[criticalThreatRoll]);
                        damageReport.Log(new AttackLogEntry
                        {
                            Number = locationTransformedDamage,
                            Location = location,
                            Type = AttackLogEntryType.Critical
                        });
                    }

                    break;
                case UnitType.VehicleHover:
                case UnitType.VehicleWheeled:
                case UnitType.VehicleTracked:
                case UnitType.Building:
                case UnitType.VehicleVtol:
                    if (criticalDamageTableType == CriticalDamageTableType.Motive)
                    {
                        switch (targetUnit.Type)
                        {
                            case UnitType.VehicleHover:
                                criticalThreatRoll += 2;
                                break;
                            case UnitType.VehicleWheeled:
                                criticalThreatRoll += 1;
                                break;
                        }

                        damageReport.Log(new AttackLogEntry
                        {
                            Context = "Critical Threat roll modified by unit type", Number = criticalThreatRoll,
                            Type = AttackLogEntryType.Calculation
                        });
                    }

                    if (criticalDamageTable.Mapping[criticalThreatRoll].Any(c => c != CriticalDamageType.None))
                    {
                        damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.Normal, criticalDamageTable.Mapping[criticalThreatRoll]);
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = string.Join(", ", criticalDamageTable.Mapping[criticalThreatRoll].Select(c => c.ToString())),
                            Number = locationTransformedDamage,
                            Location = location,
                            Type = AttackLogEntryType.Critical
                        });
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetUnit), "No critical roll handling for designated target type.");
            }
        }

        /// <summary>
        /// Resolves whether a specific cover blocks the attack or not.
        /// </summary>
        /// <param name="cover">Cover for this attack.</param>
        /// <param name="location">Location the attack would hit.</param>
        /// <param name="targetUnit">The target unit type.</param>
        /// <returns></returns>
        private bool HitIsBlockedByCover(Cover cover, Location location, UnitEntry targetUnit)
        {
            switch (targetUnit.Type)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    switch (cover)
                    {
                        case Cover.None:
                            return false;
                        case Cover.Lower:
                            switch (location)
                            {
                                case Location.LeftLeg:
                                case Location.RightLeg:
                                case Location.RearLeftLeg:
                                case Location.RearRightLeg:
                                case Location.CenterLeg:
                                    return true;
                                default:
                                    return false;
                            }
                        case Cover.Left:
                            switch (location)
                            {
                                case Location.LeftTorso:
                                case Location.RearLeftTorso:
                                case Location.LeftArm:
                                case Location.LeftLeg:
                                case Location.RearLeftLeg:
                                    return true;
                                default:
                                    return false;
                            }
                        case Cover.Right:
                            switch (location)
                            {
                                case Location.RightTorso:
                                case Location.RearRightTorso:
                                case Location.RightArm:
                                case Location.RightLeg:
                                case Location.RearRightLeg:
                                    return true;
                                default:
                                    return false;
                            }
                        case Cover.Upper:
                            switch (location)
                            {
                                case Location.Head:
                                case Location.LeftTorso:
                                case Location.RightTorso:
                                case Location.CenterTorso:
                                case Location.RearLeftTorso:
                                case Location.RearRightTorso:
                                case Location.RearCenterTorso:
                                case Location.LeftArm:
                                case Location.RightArm:
                                    return true;
                                default:
                                    return false;
                            }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(cover), cover, null);
                    }
                default:
                    return false;
            }
        }

        private int TransformDamageAmountBasedOnArmor(DamageReport damageReport, Location location, UnitEntry targetUnit, int damagePacketDamage)
        {
            // TODO: Reactive, Reflective and other special armor types also transform their damage here
            return damagePacketDamage;
        }

        private int TransformDamageAmountBasedOnLocation(DamageReport damageReport, Location location, UnitEntry targetUnit, int damagePacketDamage)
        {
            // Only one case for now
            if (targetUnit.Type == UnitType.VehicleVtol && location == Location.Propulsion)
            {
                var damage = decimal.ToInt32(Math.Ceiling(damagePacketDamage / 10m));
                damageReport.Log(new AttackLogEntry { Context = "Damage after transformation into VTOL propulsion damage", Number=damage, Type = AttackLogEntryType.Calculation } );
                return damage;
            }

            return damagePacketDamage;
        }

        private (Location, CriticalDamageTableType) GetLocation(DamageReport damageReport, Cover cover, PaperDoll paperDoll, UnitEntry targetUnit)
        {
            var (hitLocation, location) = RollHitLocation(damageReport, cover, paperDoll, targetUnit, false);

            if (location == Location.Reroll)
            {
                damageReport.Log(new AttackLogEntry { Context = "Location will be rerolled", Type = AttackLogEntryType.Information });
                (_, location) = RollHitLocation(damageReport, cover, paperDoll, targetUnit, true);
            }

            var criticalDamageTableType = paperDoll.CriticalDamageMapping[hitLocation];

            return (location, criticalDamageTableType);
        }

        private (int hitLocation, Location location) RollHitLocation(DamageReport damageReport, Cover cover, PaperDoll paperDoll, UnitEntry targetUnit, bool forceValidHit)
        {
            Location location;
            int hitLocation;
            var ready = false;

            do
            {
                hitLocation = paperDoll.LocationMapping.Keys.Count switch
                {
                    11 => _random.D26(),
                    _ => _random.NextPlusOne(paperDoll.LocationMapping.Keys.Count)
                };

                damageReport.Log(new AttackLogEntry {Context = "Location", Number = hitLocation, Type = AttackLogEntryType.DiceRoll});

                var locationList = paperDoll.LocationMapping[hitLocation];

                // Return a random entry from the location list, if it has more than one entry
                location = locationList[_random.Next(locationList.Count)];

                if (locationList.Count != 1)
                {
                    damageReport.Log(new AttackLogEntry
                    {
                        Context = $"Selecting {location} from {locationList.Count} possible entries for this hit location",
                        Type = AttackLogEntryType.Information
                    });
                }

                if (forceValidHit)
                {
                    if (location == Location.Reroll)
                    {
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = $"Location {location} will be rerolled as a valid hit is required",
                            Type = AttackLogEntryType.Information
                        });
                    }
                    else if (HitIsBlockedByCover(cover, location, targetUnit))
                    {
                        damageReport.Log(new AttackLogEntry
                        {
                            Context = $"Location {location} will be rerolled as it would be blocked by cover and a valid hit is required",
                            Type = AttackLogEntryType.Information
                        });
                    }
                    else
                    {
                        ready = true;
                    }
                }
                else
                {
                    ready = true;
                }
            } while (!ready);

            return (hitLocation, location);
        }

        /// <summary>
        /// Helper method for paper doll selection, based on attack parameters.
        /// Needed because not all units have their individual paperdoll.
        /// </summary>
        /// <param name="targetType">The UnitType of the target.</param>
        /// <param name="criticalDamageTableType">The type of the critical damage table to use.</param>
        /// <param name="location">The location the attack struck.</param>
        /// <returns></returns>
        private static string GetCriticalDamageTableName(UnitType targetType, CriticalDamageTableType criticalDamageTableType, Location location)
        {
            var transformedTargetType = TransformTargetTypeToPaperDollType(targetType);

            Location transformedLocation;

            if (targetType == UnitType.Mech || targetType == UnitType.Building)
            {
                transformedLocation = Location.Front;
            }
            else
            {
                transformedLocation = location;
            }

            return CriticalDamageTable.GetIdFromProperties(transformedTargetType, criticalDamageTableType, transformedLocation);
        }
    }
}