using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.States;

/// <summary>
/// The internal state of a game actor.
/// </summary>
[Serializable]
public class GameActorState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameActorState"/> class.
    /// </summary>
    public GameActorState()
    {
        TimeStamp = DateTime.UtcNow;
        PlayerIds = new HashSet<string>();
        DamageReports = new DamageReportCollection();
        Options = new GameOptions();
        Password = string.Empty;
        PlayerStates = new SortedDictionary<string, PlayerState>();
    }

    /// <summary>
    /// The ID of the game administrator.
    /// </summary>
    public string AdminId { get; set; }

    /// <summary>
    /// The players in the game.
    /// </summary>
    public HashSet<string> PlayerIds { get; set; }

    /// <summary>
    /// The damage reports.
    /// </summary>
    public DamageReportCollection DamageReports { get; set; }

    /// <summary>
    /// The game options.
    /// </summary>
    public GameOptions Options { get; set; }

    /// <summary>
    /// The game password.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// The player states.
    /// </summary>
    public SortedDictionary<string, PlayerState> PlayerStates { get; set; }

    /// <summary>
    /// The update timestamp.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The turn the game is on.
    /// </summary>
    public int Turn { get; set; }

    /// <summary>
    /// The timestamp for when the turn was processed.
    /// </summary>
    public DateTime TurnTimeStamp { get; set; }

    /// <summary>
    /// Reset the game turn to 0.
    /// </summary>
    public void Reset()
    {
        DamageReports.Clear();
        TurnTimeStamp = DateTime.UtcNow;
        Turn = 0;
    }
}