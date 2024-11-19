using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces;

/// <summary>
/// An interface for providing stateful logging methods for grains.
/// </summary>
public interface ILoggingService : IGrainService
{
    /// <summary>
    /// Logs a game action.
    /// </summary>
    /// <param name="timeStamp">The timestamp of the action.</param>
    /// <param name="gameId">The game ID.</param>
    /// <param name="gameActionType">The game action type.</param>
    /// <param name="actionData">The action data.</param>
    /// <returns>A task which finishes when the item has been logged.</returns>
    public Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData);

    /// <summary>
    /// Logs a player action.
    /// </summary>
    /// <param name="timeStamp">The timestamp of the action.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="playerActionType">The player action type.</param>
    /// <param name="actionData">The action data.</param>
    /// <returns>A task which finishes when the item has been logged.</returns>
    public Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData);
}