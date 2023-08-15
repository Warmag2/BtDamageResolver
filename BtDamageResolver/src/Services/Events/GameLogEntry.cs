using System;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;

namespace Faemiyah.BtDamageResolver.Services.Events;

/// <summary>
/// The game log entry.
/// </summary>
[Serializable]
public class GameLogEntry
{
    /// <summary>
    /// The timestamp.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The game ID.
    /// </summary>
    public string GameId { get; set; }

    /// <summary>
    /// The type of the log action.
    /// </summary>
    public GameActionType ActionType { get; set; }

    /// <summary>
    /// Action data, if any.
    /// </summary>
    public int ActionData { get; set; }
}