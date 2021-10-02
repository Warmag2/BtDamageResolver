using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors
{
    public class UnitActor : Grain, IUnitActor
    {
        private readonly ILogger<UnitActor> _logger;
        private readonly ILoggingServiceClient _loggingServiceClient;
        private readonly IPersistentState<UnitActorState> _unitActorState;
        private readonly ILogicCombat _logicCombat;
        private readonly ILogicHitModifier _logicHitModifier;

        /// <summary>
        /// Constructor for an Unit actor.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="loggingServiceClient">The logging service client.</param>
        /// <param name="unitActorState">The state object for this actor.</param>
        /// <param name="logicCombat">The combat logic class.</param>
        /// <param name="logicHitModifier">The specific combat logic class for hit modifier calculation.</param>
        public UnitActor(
            ILogger<UnitActor> logger,
            ILoggingServiceClient loggingServiceClient,
            [PersistentState(nameof(UnitActorState), Settings.ActorStateStoreName)]IPersistentState<UnitActorState> unitActorState,
            ILogicCombat logicCombat,
            ILogicHitModifier logicHitModifier)
        {
            _logger = logger;
            _loggingServiceClient = loggingServiceClient;
            _unitActorState = unitActorState;
            _logicCombat = logicCombat;
            _logicHitModifier = logicHitModifier;
        }

        /// <inheritdoc />
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
        }

        /// <inheritdoc />
        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        /// <inheritdoc />
        public async Task<List<DamageReport>> ProcessFireEvent(GameOptions gameOptions)
        {
            _logger.LogInformation("Unit {unit} performing fire event.", this.GetPrimaryKey());

            // Log to permanent store
            await _loggingServiceClient.LogUnitAction(DateTime.UtcNow, this.GetPrimaryKey().ToString(), UnitActionType.Fire, 1);

            return await _logicCombat.Fire(gameOptions, _unitActorState.State.UnitEntry);
        }

        public async Task<List<TargetNumberUpdate>> ProcessTargetNumbers(GameOptions gameOptions, bool setBlankNumbers)
        {
            var targetNumberUpdates = new List<TargetNumberUpdate>();

            foreach (var weaponEntry in _unitActorState.State.UnitEntry.Weapons)
            {
                if (setBlankNumbers)
                {
                    targetNumberUpdates.Add(new TargetNumberUpdate
                    {
                        CalculationLog = new AttackLog(),
                        TargetNumber = LogicConstants.InvalidTargetNumber,
                        UnitId = this.GetPrimaryKey(),
                        WeaponEntryId = weaponEntry.Id
                    });
                }
                else
                {
                    var targetUnit = await GrainFactory.GetGrain<IUnitActor>(_unitActorState.State.UnitEntry.FiringSolution.TargetUnit).GetUnitState();
                    var weapon = await GrainFactory.GetWeaponRepository().Get(weaponEntry.WeaponName);
                    var attackLog = new AttackLog();
                    (var targetNumber, _) = _logicHitModifier.ResolveHitModifier(attackLog, gameOptions, _unitActorState.State.UnitEntry, targetUnit, weapon, weaponEntry.Mode);

                    targetNumberUpdates.Add(new TargetNumberUpdate
                    {
                        CalculationLog = attackLog,
                        TargetNumber = targetNumber,
                        UnitId = this.GetPrimaryKey(),
                        WeaponEntryId = weaponEntry.Id
                    });
                }
            }

            _logger.LogInformation("UnitActor {unitId} finished calculating new target number values.", this.GetPrimaryKey());

            return targetNumberUpdates;
        }

        /// <inheritdoc />
        public Task<UnitEntry> GetUnitState()
        {
            return Task.FromResult(_unitActorState.State.UnitEntry);
        }

        public async Task<DamageReport> ProcessDamageInstance(DamageInstance damageInstance, GameOptions gameOptions)
        {
            return await _logicCombat.ResolveDamageInstance(damageInstance, gameOptions);
        }

        /// <inheritdoc />
        public async Task<bool> SendState(UnitEntry unit)
        {
            if (unit.TimeStamp > _unitActorState.State.UpdateTimeStamp)
            {
                _logger.LogInformation("Updating unit data for unit {unit} with new data from {timestamp}.", unit.Id, unit.TimeStamp);

                _unitActorState.State.UpdateTimeStamp = unit.TimeStamp;
                _unitActorState.State.UnitEntry = unit;
                _unitActorState.State.Initialized = true;
                await _unitActorState.WriteStateAsync();

                // Log to permanent store
                await _loggingServiceClient.LogUnitAction(DateTime.UtcNow, this.GetPrimaryKey().ToString(), UnitActionType.Update, 1);

                return true;
            }

            _logger.LogDebug(
                "Discarding update event for unit {unitId}. Timestamp {stampEvent}, is older than existing timestamp {stampState}.",
                unit.Id, unit.TimeStamp, _unitActorState.State.UpdateTimeStamp);
            return false;
        }
    }
}