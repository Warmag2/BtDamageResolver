using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.Cryptography;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors;

/// <summary>
/// Partial class for player actor containing the base implementation.
/// </summary>
public partial class PlayerActor : Grain, IPlayerActor
{
    private readonly ILogger<PlayerActor> _logger;
    private readonly ICommunicationServiceClient _communicationServiceClient;
    private readonly IHasher _hasher;
    private readonly ILoggingServiceClient _loggingServiceClient;
    private readonly IPersistentState<PlayerActorState> _playerActorState;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerActor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="communicationServiceClient">The communication service client.</param>
    /// <param name="hasher">The password hasher.</param>
    /// <param name="loggingServiceClient">The logging service client.</param>
    /// <param name="playerActorState">The state object for this actor.</param>
    public PlayerActor(
        ILogger<PlayerActor> logger,
        ICommunicationServiceClient communicationServiceClient,
        IHasher hasher,
        ILoggingServiceClient loggingServiceClient,
        [PersistentState(nameof(PlayerActorState), Settings.ActorStateStoreName)]IPersistentState<PlayerActorState> playerActorState)
    {
        _logger = logger;
        _communicationServiceClient = communicationServiceClient;
        _hasher = hasher;
        _loggingServiceClient = loggingServiceClient;
        _playerActorState = playerActorState;
    }

    /// <inheritdoc />
    public async Task<string> GetGameId(Guid authenticationToken)
    {
        if (!await CheckAuthentication(authenticationToken))
        {
            return null;
        }

        return _playerActorState.State.GameId;
    }

    /// <inheritdoc />
    public async Task<PlayerState> GetPlayerState(bool markStateAsNew)
    {
        if (markStateAsNew)
        {
            _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
        }

        var units = new List<UnitEntry>();
        foreach (var unitId in _playerActorState.State.UnitEntryIds)
        {
            units.Add(await GrainFactory.GetGrain<IUnitActor>(unitId).GetUnit());
        }

        var playerState = new PlayerState
        {
            IsReady = _playerActorState.State.IsReady,
            PlayerId = this.GetPrimaryKeyString(),
            TimeStamp = _playerActorState.State.UpdateTimeStamp,
            UnitEntries = units
        };

        return playerState;
    }

    /// <inheritdoc />
    public async Task UnReady()
    {
        _playerActorState.State.IsReady = false;
        _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
        await _playerActorState.WriteStateAsync();
    }
}