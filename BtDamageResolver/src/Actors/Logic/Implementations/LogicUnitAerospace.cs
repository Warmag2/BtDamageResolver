using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Entities;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Implementations
{
    /// <summary>
    /// Abstract base logic class for all aerospace units.
    /// </summary>
    public abstract class LogicUnitAerospace : LogicUnit
    {
        /// <inheritdoc />
        public LogicUnitAerospace(ILogger<LogicUnitAerospace> logger, LogicHelper logicHelper, GameOptions options, UnitEntry unit) : base(logger, logicHelper, options, unit)
        {
        }

        /// <inheritdoc />
        public override int GetFeatureModifier(Weapon weapon, WeaponMode mode)
        {
            if (weapon.SpecialFeatures[mode].HasFeature(WeaponFeature.Flak, out var flakFeatureEntry))
            {
                return LogicHelper.MathExpression.Parse(flakFeatureEntry.Data);
            }

            return 0;
        }

        /// <inheritdoc />
        protected override int GetMinimumRangeModifier(Weapon weapon, WeaponMode mode)
        {
            return 0;
        }

        /// <inheritdoc />
        public override int GetMovementDirectionModifier(Direction direction)
        {
            switch (direction)
            {
                case Direction.Rear:
                    return 0;
                case Direction.Front:
                    return 1;
                case Direction.Left:
                case Direction.Right:
                case Direction.Bottom:
                case Direction.Top:
                    return 2;
                default:
                    throw new InvalidOperationException($"Unexpected direction: {direction}");
            }
        }

        /// <inheritdoc />
        public override int GetMovementModifier()
        {
            return 0;
        }

        /// <inheritdoc />
        protected override int GetOwnMovementModifier()
        {
            return Unit.MovementClass == MovementClass.OutOfControl || Unit.MovementClass == MovementClass.Fast ? 2 : 0;
        }

        /// <inheritdoc />
        protected override RangeBracket GetRangeBracket(Weapon weapon)
        {
            return GetRangeBracketAerospace(weapon, Unit.FiringSolution.Distance);
        }

        /// <inheritdoc />
        protected override List<DamagePacket> ResolveDamagePackets(DamageReport damageReport, ILogicUnit target, CombatAction combatAction, int damage)
        {
            // Missile weapons which do 0 damage have been shot down. Return an empty list.
            if (combatAction.Weapon.Type == WeaponType.Missile && damage == 0)
            {
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile weapon has been shot down and does no damage" });
                return new List<DamagePacket>();
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _))
            {
                return Clusterize(1, 5, damage, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
            }

            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Rapid, out var rapidFeatureEntry))
            {
                return Clusterize(1, (int)Math.Ceiling((decimal)damage / LogicHelper.MathExpression.Parse(rapidFeatureEntry.Data)), damage, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
            }

            return Clusterize(1, damage, damage, combatAction.Weapon.SpecialDamage[combatAction.WeaponMode]);
        }

        /// <inheritdoc />
        protected override async Task ResolveCriticalHit(DamageReport damageReport, Location location, int criticalThreatRoll, int inducingDamage, int transformedDamage, CriticalDamageTableType criticalDamageTableType)
        {
            var criticalDamageTableId = GetCriticalDamageTableName(this, criticalDamageTableType, location);
            var criticalDamageTable = await LogicHelper.GrainFactory.GetCriticalDamageTableRepository().Get(criticalDamageTableId);

            if (criticalThreatRoll > 7)
            {
                var aerospaceCriticalHitRoll = LogicHelper.Random.D26();
                damageReport.Log(new AttackLogEntry
                {
                    Context = "Aerospace critical hit roll",
                    Number = aerospaceCriticalHitRoll,
                    Type = AttackLogEntryType.DiceRoll
                });

                damageReport.DamagePaperDoll.RecordCriticalDamage(location, inducingDamage, CriticalThreatType.DamageThreshold, criticalDamageTable.Mapping[aerospaceCriticalHitRoll]);
                damageReport.Log(new AttackLogEntry
                {
                    Context = string.Join(", ", criticalDamageTable.Mapping[aerospaceCriticalHitRoll].Select(c => c.ToString())),
                    Number = transformedDamage,
                    Location = location,
                    Type = AttackLogEntryType.Critical
                });
            }
        }

        /// <inheritdoc />
        protected override Task<int> ResolveTotalOutgoingDamage(DamageReport damageReport, ILogicUnit target, CombatAction combatAction)
        {
            var damageValue = 0;

            switch (combatAction.Weapon.Type)
            {
                case WeaponType.Missile:
                    if (target.GetUnit().HasFeature(UnitFeature.Ams))
                    {
                        if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.AmsImmune, out _))
                        {
                            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Information, Context = "Missile is immune to AMS defenses" });
                        }
                        else
                        {
                            var amsPenalty = LogicHelper.Random.Next(6);
                            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender AMS roll for cluster damage reduction", Number = amsPenalty });
                            damageValue -= amsPenalty;

                            damageReport.SpendAmmoDefender("AMS", 1);
                        }
                    }

                    if (target.GetUnit().HasFeature(UnitFeature.Ecm) && !Unit.HasFeature(UnitFeature.Bap))
                    {
                        var ecmPenalty = LogicHelper.Random.Next(3);
                        damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender ECM roll for cluster damage reduction", Number = ecmPenalty });
                        damageValue -= ecmPenalty;
                    }
                    break;
            }

            // Glancing blow for cluster aerospace weapons (improvised rule, since aerospace units do not normally use clustering)
            if (combatAction.Weapon.SpecialFeatures[combatAction.WeaponMode].HasFeature(WeaponFeature.Cluster, out _) && target.IsGlancingBlow(combatAction.MarginOfSuccess))
            {
                var glancingBlowPenalty = LogicHelper.Random.Next(6);
                damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.DiceRoll, Context = "Defender roll for cluster damage reduction from glancing blow", Number = glancingBlowPenalty });
                damageValue -= glancingBlowPenalty;
            }

            damageValue += Math.Clamp(combatAction.Weapon.DamageAerospace[combatAction.RangeBracket], 0, int.MaxValue);
            damageReport.Log(new AttackLogEntry { Type = AttackLogEntryType.Calculation, Context = "Total damage value", Number = damageValue });

            return Task.FromResult(damageValue);
        }
    }
}
