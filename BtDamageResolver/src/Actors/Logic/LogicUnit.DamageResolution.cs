using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Logic unit methods that concern damage packet application, i.e. damage resolution.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public async Task ApplyDamagePackets(DamageReport damageReport, List<DamagePacket> damagePackets, FiringSolution firingSolution, int marginOfSuccess)
    {
        foreach (var damagePacket in damagePackets)
        {
            var (location, criticalDamageTableType) = GetLocation(damageReport, firingSolution.Cover, damageReport.DamagePaperDoll.PaperDoll);

            if (IsBlockedByCover(firingSolution.Cover, location))
            {
                damageReport.Log(new AttackLogEntry { Context = $"Hit to {location} blocked by cover", Type = AttackLogEntryType.Information });

                continue;
            }

            // Unfortunately we still have to do transformations at this point, as certain locations and armor types receive damage differently
            var transformedDamage = TransformDamageAmountBasedOnLocation(damageReport, location, damagePacket.Damage);

            damageReport.DamagePaperDoll.RecordDamage(location, transformedDamage);
            damageReport.Log(new AttackLogEntry { Location = location, Number = transformedDamage, Type = AttackLogEntryType.Damage });

            foreach (var specialDamageEntry in damagePacket.SpecialDamageEntries)
            {
                if (specialDamageEntry.Type != SpecialDamageType.None)
                {
                    switch (specialDamageEntry.Type)
                    {
                        case SpecialDamageType.Critical:
                        case SpecialDamageType.Motive:
                            damageReport.Log(new AttackLogEntry { Context = "Weapon special damage induces a critical damage threat", Type = AttackLogEntryType.Information });

                            var criticalTableType = specialDamageEntry.Type == SpecialDamageType.Critical ? CriticalDamageTableType.Critical : CriticalDamageTableType.Motive;

                            var criticalDamageTable = await GetCriticalDamageTable(criticalTableType, location);

                            // Critical rolls from damage may have a modifier.
                            var specialDamageThreatModifier = MathExpression.Parse(specialDamageEntry.Data);
                            damageReport.Log(new AttackLogEntry { Context = "Threat roll modifier from damage source", Number = specialDamageThreatModifier, Type = AttackLogEntryType.Calculation });
                            var glancingBlowModifier = IsGlancingBlow(marginOfSuccess) ? -2 : 0;
                            if (glancingBlowModifier != 0)
                            {
                                damageReport.Log(new AttackLogEntry { Context = "Threat roll glancing blow modifier", Number = glancingBlowModifier, Type = AttackLogEntryType.Calculation });
                            }

                            var specialDamageEntryCriticalThreatRoll = Math.Clamp(Random.D26() + specialDamageThreatModifier + glancingBlowModifier, 2, 12);
                            damageReport.Log(new AttackLogEntry { Context = "Threat roll", Number = specialDamageEntryCriticalThreatRoll, Type = AttackLogEntryType.DiceRoll });

                            if (criticalDamageTable.Mapping[specialDamageEntryCriticalThreatRoll].Exists(c => c != CriticalDamageType.None))
                            {
                                // Critical damage threats coming from special damage (AP Ammo, Retractable Blade) never do multiple critical effects. Take the first.
                                // Direct critical damage threats should always succeed, regardless of damage threshold. Give a very high damage amount.
                                var criticalDamageType = criticalDamageTable.Mapping[specialDamageEntryCriticalThreatRoll][0];
                                damageReport.DamagePaperDoll.RecordCriticalDamage(location, damagePacket.Damage, CriticalThreatType.Normal, criticalDamageType);
                                damageReport.Log(new AttackLogEntry { Context = criticalDamageType.ToString(), Number = 0, Location = location, Type = AttackLogEntryType.Critical });
                            }
                            else
                            {
                                damageReport.Log(new AttackLogEntry
                                {
                                    Context = "Threat roll does not result in a critical hit",
                                    Type = AttackLogEntryType.Information
                                });
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
                var criticalThreatRoll = Random.D26();

                if (criticalDamageTableType == CriticalDamageTableType.Motive)
                {
                    damageReport.Log(new AttackLogEntry
                    {
                        Context = "Motive Hit threat",
                        Number = criticalThreatRoll,
                        Type = AttackLogEntryType.DiceRoll
                    });
                }
                else
                {
                    damageReport.Log(new AttackLogEntry
                    {
                        Context = "Critical Hit threat",
                        Number = criticalThreatRoll,
                        Type = AttackLogEntryType.DiceRoll
                    });
                }

                await ResolveCriticalHit(damageReport, location, criticalThreatRoll, damagePacket.Damage, transformedDamage, criticalDamageTableType);
            }
        }
    }

    /// <summary>
    /// Resolve a critical hit.
    /// </summary>
    /// <param name="damageReport">The damage report to append to.</param>
    /// <param name="location">The location the hit occurs in.</param>
    /// <param name="criticalThreatRoll">The critical threat roll.</param>
    /// <param name="inducingDamage">The inducing damage.</param>
    /// <param name="transformedDamage">The transformed damage (location, armor).</param>
    /// <param name="criticalDamageTableType">The critical damage table type.</param>
    /// <returns>A task which finishes when the critical hit has been resolved.</returns>
    protected virtual Task ResolveCriticalHit(DamageReport damageReport, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
    {
        damageReport.Log(new AttackLogEntry { Context = "Unit type does not take critical hits", Type = AttackLogEntryType.Information });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Transform damage based on hit location.
    /// </summary>
    /// <param name="damageReport">The damage report to append to.</param>
    /// <param name="location">The location.</param>
    /// <param name="damage">The amount of damage before transformation.</param>
    /// <returns>The transformed damage amount.</returns>
    protected virtual int TransformDamageAmountBasedOnLocation(DamageReport damageReport, Location location, int damage)
    {
        return damage;
    }

    private (Location Location, CriticalDamageTableType CriticalDamageTableType) GetLocation(DamageReport damageReport, Cover cover, PaperDoll paperDoll)
    {
        var (hitLocation, location) = RollHitLocation(damageReport, cover, paperDoll, false);

        if (location == Location.Reroll)
        {
            damageReport.Log(new AttackLogEntry { Context = "Location will be rerolled", Type = AttackLogEntryType.Information });
            (_, location) = RollHitLocation(damageReport, cover, paperDoll, true);
        }

        var criticalDamageTableType = paperDoll.CriticalDamageMapping[hitLocation];

        return (location, criticalDamageTableType);
    }

    private (int HitLocation, Location Location) RollHitLocation(DamageReport damageReport, Cover cover, PaperDoll paperDoll, bool forceValidHit)
    {
        Location location;
        int hitLocation;
        var ready = false;

        do
        {
            hitLocation = paperDoll.LocationMapping.Keys.Count switch
            {
                11 => Random.D26(),
                _ => Random.NextPlusOne(paperDoll.LocationMapping.Keys.Count)
            };

            damageReport.Log(new AttackLogEntry { Context = "Location", Number = hitLocation, Type = AttackLogEntryType.DiceRoll });

            var locationList = paperDoll.LocationMapping[hitLocation];

            // Return a random entry from the location list, if it has more than one entry
            location = locationList[Random.Next(locationList.Count)];

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
                else if (IsBlockedByCover(cover, location))
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
        }
        while (!ready);

        return (hitLocation, location);
    }
}
