using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.States.Types;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Partial class for player actor containing data transfer methods.
/// </summary>
public partial class PlayerActor
{
    /// <inheritdoc />
    public async Task<bool> SendDamageInstance(Guid authenticationToken, DamageInstance damageInstance)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        if (IsConnectedToGame())
        {
            var gameActor = GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId);

            if (await gameActor.IsUnitInGame(damageInstance.UnitId))
            {
                return await gameActor.SendDamageInstance(this.GetPrimaryKeyString(), damageInstance);
            }

            _logger.LogWarning("Player {PlayerId} asked for a damage request against unit {UnitId}, but the said unit is not in the game.", this.GetPrimaryKeyString(), damageInstance.UnitId);

            return false;
        }

        _logger.LogWarning("Player {PlayerId} asked for a damage request, but is not connected to a game.", this.GetPrimaryKeyString());

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> SendGameOptions(Guid authenticationToken, GameOptions gameOptions)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _logger.LogInformation("Player {PlayerId} is trying to set game options.", this.GetPrimaryKeyString());

        return
            _playerActorState.State.GameId != null &&
            await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).SendGameOptions(this.GetPrimaryKeyString(), gameOptions);
    }

    /// <inheritdoc />
    public async Task<bool> SendPlayerState(Guid authenticationToken, PlayerState playerState)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        // Validate unit entries and do not proceed if they contain data that would compromise the rules engine.
        // This player state is not updated with invalid state.
        foreach (var unit in playerState.UnitEntries)
        {
            var validationResult = unit.Validate();

            if (!validationResult.IsValid)
            {
                await SendErrorMessageToClient($"Unit {unit} has the following errors: {validationResult}");
                _logger.LogWarning("Player {PlayerId} is sending invalid data for unit {UnitId}. Reason: {ValidationResult}", this.GetPrimaryKeyString(), unit.Id, validationResult);

                return false;
            }
        }

        var success = false;

        try
        {
            if (playerState.TimeStamp > _playerActorState.State.UpdateTimeStamp)
            {
                var updatedUnits = _playerActorState.State.UnitEntries.AreNewOrNewer(playerState.UnitEntries);

                _logger.LogInformation("Updating player {PlayerId} state with new data from {Timestamp}", this.GetPrimaryKeyString(), playerState.TimeStamp);
                _playerActorState.State.IsReady = playerState.IsReady;
                _playerActorState.State.UpdateTimeStamp = playerState.TimeStamp;
                _playerActorState.State.UnitEntries = new UnitList(playerState.UnitEntries);

                // Save this state first and wait for save to finish to avoid any race conditions
                // arising from changes incurred by uploading the state to the game actor.
                await _playerActorState.WriteStateAsync();

                if (IsConnectedToGame())
                {
                    // If we are connected to the game, also push player state to the game actor to be distributed to other players.
                    success = await GrainFactory.GetGrain<IGameActor>(_playerActorState.State.GameId).SendPlayerState(this.GetPrimaryKeyString(), playerState, updatedUnits);
                }

                // Log the number of updated units to permanent store
                await _loggingServiceClient.LogPlayerAction(DateTime.UtcNow, this.GetPrimaryKeyString(), PlayerActionType.UpdateUnit, updatedUnits.Count);
            }
            else
            {
                _logger.LogInformation(
                    "Discarding update event for player {PlayerId}. Timestamp {StampEvent}, is older than existing timestamp {StampState}.",
                    this.GetPrimaryKeyString(),
                    playerState.TimeStamp,
                    _playerActorState.State.UpdateTimeStamp);
            }
        }
        catch (Exception ex)
        {
            await SendErrorMessageToClient($"{ex.Message}\n{ex.StackTrace}");
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> SendPlayerOptions(Guid authenticationToken, PlayerOptions playerOptions)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return false;
        }

        _playerActorState.State.Options = playerOptions;
        await _playerActorState.WriteStateAsync();

        _logger.LogInformation("Player {PlayerId} updated player options.", this.GetPrimaryKeyString());

        return true;
    }
}