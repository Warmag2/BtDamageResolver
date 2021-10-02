using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.Entities;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors
{
    public partial class GameActor
    {
        /// <summary>
        /// Send all damage reports which have been recorded in this game to the designated player.
        /// </summary>
        /// <param name="playerAuthenticationToken">The authentication token of the player to send the damage reports to.</param>
        private async Task DistributeAllDamageReportsToPlayer(Guid playerAuthenticationToken)
        {
            await _communicationServiceClient.Send(_gameActorState.State.AuthenticationTokens[playerAuthenticationToken], EventNames.DamageReports, _gameActorState.State.DamageReports.GetAll());
        }

        /// <summary>
        /// Receives a set of damage reports and sends them to players.
        /// </summary>
        /// <param name="damageReports">A list of <see cref="DamageReport"/>s that are to be distributed to players.</param>
        private async Task DistributeDamageReportsToPlayers(List<DamageReport> damageReports)
        {
            _logger.LogInformation("Game {id} is sending an damage reports to all players.", this.GetPrimaryKeyString());

            await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerStates.Keys.ToList(), EventNames.DamageReports, damageReports);
        }

        /// <summary>
        /// Sends the game state update to all players.
        /// </summary>
        private async Task DistributeGameStateToPlayers()
        {
            _logger.LogInformation("Game {id} is sending a game state update to all players.", this.GetPrimaryKeyString());
            var gameState = GetGameState();

            await _communicationServiceClient.SendToMany(gameState.Players.Keys.ToList(), EventNames.GameState, gameState);
        }

        /// <summary>
        /// Sends the game state to a single player.
        /// </summary>
        private async Task DistributeGameStateToPlayer(Guid authenticationToken)
        {
            await _communicationServiceClient.Send(GetPlayerForAuthenticationToken(authenticationToken), EventNames.GameState, GetGameState());
        }

        /// <summary>
        /// Sends the game options to a players.
        /// </summary>
        private async Task DistributeGameOptionsToPlayer(string playerId)
        {
            _logger.LogInformation("Game {id} is sending an options update to player {player}.", this.GetPrimaryKeyString(), playerId);

            await _communicationServiceClient.Send(playerId, EventNames.GameOptions, _gameActorState.State.Options);
        }

        /// <summary>
        /// Sends the game options to all players.
        /// </summary>
        private async Task DistributeGameOptionsToPlayers()
        {
            _logger.LogInformation("Game {id} is sending an options update to all players.", this.GetPrimaryKeyString());

            await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerStates.Keys.ToList(), EventNames.GameOptions, _gameActorState.State.Options);
        }

        /// <summary>
        /// Sends the target number updates to a players.
        /// </summary>
        private async Task DistributeTargetNumberUpdatesToPlayer(string playerId, List<TargetNumberUpdate> targetNumberUpdates)
        {
            _logger.LogInformation("Game {id} is sending target number updates to player {player}.", this.GetPrimaryKeyString(), playerId);

            await _communicationServiceClient.Send(playerId, EventNames.TargetNumbers, targetNumberUpdates);
        }

        /// <summary>
        /// Sends the target number updates to all players.
        /// </summary>
        private async Task DistributeTargetNumberUpdatesToPlayers(List<TargetNumberUpdate> targetNumberUpdates)
        {
            _logger.LogInformation("Game {id} is sending target number updates to all players.", this.GetPrimaryKeyString());

            await _communicationServiceClient.SendToMany(_gameActorState.State.PlayerStates.Keys.ToList(), EventNames.TargetNumbers, targetNumberUpdates);
        }
    }
}