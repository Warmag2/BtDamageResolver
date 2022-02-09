using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Actors.States;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Faemiyah.BtDamageResolver.Actors
{
    /// <summary>
    /// Partial class for player actor containing the base implementation.
    /// </summary>
    public partial class PlayerActor : Grain, IPlayerActor
    {
        private readonly ILogger<PlayerActor> _logger;
        private readonly ICommunicationServiceClient _communicationServiceClient;
        private readonly ILoggingServiceClient _loggingServiceClient;
        private readonly IPersistentState<PlayerActorState> _playerActorState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerActor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="communicationServiceClient">The communication service client.</param>
        /// <param name="loggingServiceClient">The logging service client.</param>
        /// <param name="playerActorState">The state object for this actor.</param>
        public PlayerActor(
            ILogger<PlayerActor> logger,
            ICommunicationServiceClient communicationServiceClient,
            ILoggingServiceClient loggingServiceClient,
            [PersistentState(nameof(PlayerActorState), Settings.ActorStateStoreName)]IPersistentState<PlayerActorState> playerActorState)
        {
            _logger = logger;
            _communicationServiceClient = communicationServiceClient;
            _loggingServiceClient = loggingServiceClient;
            _playerActorState = playerActorState;
        }

        /// <inheritdoc />
        public async Task<PlayerState> GetPlayerState(Guid authenticationToken, bool markStateAsNew)
        {
            if (!await CheckAuthentication(authenticationToken))
            {
                return null;
            }

            return await GetPlayerState(markStateAsNew);
        }

        /// <inheritdoc />
        public async Task UnReady()
        {
            _playerActorState.State.IsReady = false;
            _playerActorState.State.UpdateTimeStamp = DateTime.UtcNow;
            await _playerActorState.WriteStateAsync();
        }

        private async Task<PlayerState> GetPlayerState(bool markStateAsNew)
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
    }
}