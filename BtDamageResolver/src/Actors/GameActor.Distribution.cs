using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.ActorInterfaces;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Events;
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
        private void DistributeAllDamageReportsToPlayer(Guid playerAuthenticationToken)
        {
            GrainFactory
                .GetGrain<IPlayerActor>(_gameActorState.State.AuthenticationTokens[playerAuthenticationToken])
                .SendDamageReportsToClient(playerAuthenticationToken, _gameActorStateEthereal.DamageReports)
                .Ignore();
        }

        /// <summary>
        /// Receives a set of damage reports and sends them to players owning the units they concern.
        /// </summary>
        /// <param name="damageReports">A list of <see cref="DamageReport"/>s that are to be distributed to players.</param>
        private void DistributeDamageReportsToPlayers(List<DamageReport> damageReports)
        {
            foreach (var playerId in _gameActorState.State.PlayerStates.Keys)
            {
                GrainFactory.GetGrain<IPlayerActor>(playerId).SendDamageReportsToClient(GetAuthenticationTokenForPlayer(playerId), damageReports).Ignore();
            }
        }

        /// <summary>
        /// Sends the game state update to all players.
        /// </summary>
        private void DistributeGameStateToPlayers()
        {
            _logger.LogInformation("Game {id} is sending a game state update to all players.", this.GetPrimaryKeyString());
            var gameState = GetGameState();

            foreach (var playerId in gameState.Players.Keys)
            {
                DistributeGameStateToPlayer(playerId, gameState);
            }
        }

        private void DistributeGameStateToPlayer(string playerId, GameState gameState)
        {
            GrainFactory.GetGrain<IPlayerActor>(playerId).SendGameStateToClient(GetAuthenticationTokenForPlayer(playerId), gameState).Ignore();
        }

        private void DistributeGameStateToPlayer(Guid authenticationToken)
        {
            GrainFactory.GetGrain<IPlayerActor>(GetPlayerForAuthenticationToken(authenticationToken)).SendGameStateToClient(authenticationToken, GetGameState()).Ignore();
        }

        /// <summary>
        /// Sends the game options to all players.
        /// </summary>
        /// <returns>Nothing.</returns>
        private void DistributeGameOptionsToPlayers()
        {
            _logger.LogInformation("Game {id} is sending an options update to all players.", this.GetPrimaryKeyString());

            foreach (var playerId in _gameActorState.State.PlayerStates.Keys)
            {
                GrainFactory.GetGrain<IPlayerActor>(playerId).SendGameOptionsToClient(GetAuthenticationTokenForPlayer(playerId), _gameActorState.State.Options).Ignore();
            }
        }

        /// <summary>
        /// Sends the target number updates to all players.
        /// </summary>
        private void DistributeTargetNumberUpdatesToPlayers(List<TargetNumberUpdate> targetNumberUpdates)
        {
            _logger.LogInformation("Game {id} is sending target number updates to all players.", this.GetPrimaryKeyString());

            foreach (var playerId in _gameActorState.State.PlayerStates.Keys)
            {
                GrainFactory.GetGrain<IPlayerActor>(playerId).SendTargetNumberUpdatesToClient(GetAuthenticationTokenForPlayer(playerId), targetNumberUpdates).Ignore();
            }
        }
    }
}