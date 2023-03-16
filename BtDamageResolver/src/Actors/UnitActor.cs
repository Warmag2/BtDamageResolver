using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.Logic;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Extensions;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// The unit actor.
/// </summary>
public class UnitActor : Grain, IUnitActor
{
    private readonly ILogger<UnitActor> _logger;
    private readonly ILoggingServiceClient _loggingServiceClient;
    private readonly IPersistentState<UnitActorState> _unitActorState;
    private readonly ILogicUnitFactory _logicUnitFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitActor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="loggingServiceClient">The logging service client.</param>
    /// <param name="unitActorState">The state object for this actor.</param>
    /// <param name="logicUnitFactory">The unit logic factory.</param>
    public UnitActor(
        ILogger<UnitActor> logger,
        ILoggingServiceClient loggingServiceClient,
        [PersistentState(nameof(UnitActorState), Settings.ActorStateStoreName)]IPersistentState<UnitActorState> unitActorState,
        ILogicUnitFactory logicUnitFactory)
    {
        _logger = logger;
        _loggingServiceClient = loggingServiceClient;
        _unitActorState = unitActorState;
        _logicUnitFactory = logicUnitFactory;
    }

    /// <inheritdoc />
    public async Task<List<DamageReport>> ProcessFireEvent(GameOptions gameOptions, bool processOnlyTags)
    {
        _logger.LogInformation("Unit {unit} performing fire event.", this.GetPrimaryKey());

        // Log to permanent store
        await _loggingServiceClient.LogUnitAction(DateTime.UtcNow, this.GetPrimaryKey().ToString(), UnitActionType.Fire, 1);

        var logicUnitAttacker = GetUnitLogic(gameOptions);
        var logicUnitDefender = await GetUnitLogic(gameOptions, _unitActorState.State.UnitEntry.FiringSolution.TargetUnit);

        return await logicUnitAttacker.ResolveCombat(logicUnitDefender, processOnlyTags);
    }

    /// <summary>
    /// Processes the target numbers for a given unit.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <param name="setBlankNumbers">Should blank numbers be set.</param>
    /// <returns>A list of target number updates.</returns>
    public async Task<TargetNumberUpdate> ProcessTargetNumbers(GameOptions gameOptions, bool setBlankNumbers = false)
    {
        var targetNumberUpdate = new TargetNumberUpdate()
        {
            AmmoEstimate = new Dictionary<string, double>(),
            AmmoWorstCase = new Dictionary<string, int>(),
            TargetNumbers = new Dictionary<Guid, TargetNumberUpdateSingleWeapon>(),
            TimeStamp = DateTime.UtcNow,
            UnitId = this.GetPrimaryKey()
        };

        var logicUnitAttacker = GetUnitLogic(gameOptions);

        ILogicUnit logicUnitDefender = null;

        if (!setBlankNumbers)
        {
            logicUnitDefender = await GetUnitLogic(gameOptions, _unitActorState.State.UnitEntry.FiringSolution.TargetUnit);
        }

        foreach (var weaponEntry in _unitActorState.State.UnitEntry.Weapons.Where(w => w.State == WeaponState.Active))
        {
            if (!setBlankNumbers)
            {
                var attackLog = new AttackLog();

                (var targetNumber, _) = await logicUnitAttacker.ResolveHitModifier(attackLog, logicUnitDefender, weaponEntry);

                (var ammoEstimate, var ammoMax) = await logicUnitAttacker.ProjectAmmo(targetNumber, weaponEntry);
                (var heatEstimate, var heatMax) = await logicUnitAttacker.ProjectHeat(targetNumber, weaponEntry);

                string weaponAmmoCombinedString;

                if (!string.IsNullOrEmpty(weaponEntry.Ammo))
                {
                    weaponAmmoCombinedString = $"{weaponEntry.WeaponName} {weaponEntry.Ammo}";
                }
                else
                {
                    weaponAmmoCombinedString = $"{weaponEntry.WeaponName}";
                }

                targetNumberUpdate.AmmoEstimate.AddIfNotZero(weaponAmmoCombinedString, ammoEstimate);
                targetNumberUpdate.AmmoWorstCase.AddIfNotZero(weaponAmmoCombinedString, ammoMax);

                if (logicUnitAttacker.IsHeatTracking())
                {
                    targetNumberUpdate.HeatEstimate += heatEstimate;
                    targetNumberUpdate.HeatWorstCase += heatMax;
                }

                targetNumberUpdate.TargetNumbers.Add(
                    weaponEntry.Id,
                    new TargetNumberUpdateSingleWeapon
                    {
                        CalculationLog = attackLog,
                        TargetNumber = targetNumber,
                    });
            }
            else
            {
                targetNumberUpdate.TargetNumbers.Add(
                    weaponEntry.Id,
                    new TargetNumberUpdateSingleWeapon
                    {
                        CalculationLog = new AttackLog(),
                        TargetNumber = LogicConstants.InvalidTargetNumber,
                    });
            }
        }

        if (logicUnitAttacker.IsHeatTracking())
        {
            var nonWeaponHeatDamageReport = await logicUnitAttacker.ResolveNonWeaponHeat();

            targetNumberUpdate.HeatEstimate += nonWeaponHeatDamageReport.AttackerHeat;
            targetNumberUpdate.HeatWorstCase += nonWeaponHeatDamageReport.AttackerHeat;
        }

        _logger.LogInformation("UnitActor {unitId} finished calculating new target number values.", this.GetPrimaryKey());

        return targetNumberUpdate;
    }

    /// <inheritdoc />
    public Task<UnitEntry> GetUnit()
    {
        return Task.FromResult(_unitActorState.State.UnitEntry);
    }

    /// <inheritdoc />
    public async Task<DamageReport> ProcessDamageInstance(DamageInstance damageInstance, GameOptions gameOptions)
    {
        var logicUnit = GetUnitLogic(gameOptions);

        return await logicUnit.ResolveDamageInstance(damageInstance, Phase.End, false);
    }

    /// <inheritdoc />
    public async Task<bool> SendState(UnitEntry unit)
    {
        if (unit.TimeStamp > _unitActorState.State.TimeStamp)
        {
            _logger.LogInformation("Updating unit data for unit {unit} with new data from {timestamp}.", unit.Id, unit.TimeStamp);

            _unitActorState.State.TimeStamp = unit.TimeStamp;
            _unitActorState.State.UnitEntry = unit;
            _unitActorState.State.Initialized = true;
            await _unitActorState.WriteStateAsync();

            // Log to permanent store
            await _loggingServiceClient.LogUnitAction(DateTime.UtcNow, this.GetPrimaryKey().ToString(), UnitActionType.Update, 1);

            return true;
        }

        _logger.LogDebug(
            "Discarding update event for unit {unitId}. Timestamp {stampEvent}, is older than existing timestamp {stampState}.",
            unit.Id,
            unit.TimeStamp,
            _unitActorState.State.TimeStamp);
        return false;
    }

    /// <summary>
    /// Get the logic corresponding to this unit actor.
    /// </summary>
    /// <param name="gameOptions">The game options.</param>
    /// <returns>The <see cref="ILogicUnit"/> object containing the unit logic of the unit represented by this <see cref="IUnitActor"/>.</returns>
    private ILogicUnit GetUnitLogic(GameOptions gameOptions)
    {
        return _logicUnitFactory.CreateFrom(gameOptions, _unitActorState.State.UnitEntry);
    }

    /// <summary>
    /// Get the logic corresponding to the unit entry for the given unit actor.
    /// </summary>
    /// <returns>The <see cref="ILogicUnit"/> object containing the unit logic of the unit represented by this <see cref="IUnitActor"/>.</returns>
    private async Task<ILogicUnit> GetUnitLogic(GameOptions gameOptions, Guid unitId)
    {
        return _logicUnitFactory.CreateFrom(gameOptions, await GrainFactory.GetGrain<IUnitActor>(unitId).GetUnit());
    }
}