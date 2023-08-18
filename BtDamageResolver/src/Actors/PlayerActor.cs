using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.Cryptography;
using Faemiyah.BtDamageResolver.Actors.States;
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
    /// <param name="playerActorState">The state object for this actor.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="communicationServiceClient">The communication service client.</param>
    /// <param name="hasher">The password hasher.</param>
    /// <param name="loggingServiceClient">The logging service client.</param>
    public PlayerActor(
        [PersistentState(nameof(PlayerActorState), Settings.ActorStateStoreName)] IPersistentState<PlayerActorState> playerActorState,
        ILogger<PlayerActor> logger,
        ICommunicationServiceClient communicationServiceClient,
        IHasher hasher,
        ILoggingServiceClient loggingServiceClient)
    {
        _playerActorState = playerActorState;
        _logger = logger;
        _communicationServiceClient = communicationServiceClient;
        _hasher = hasher;
        _loggingServiceClient = loggingServiceClient;
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
}