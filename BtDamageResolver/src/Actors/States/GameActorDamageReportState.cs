using System;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.States;

/// <summary>
/// The internal state of a game actor.
/// </summary>
[Serializable]
public class GameActorDamageReportState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameActorDamageReportState"/> class.
    /// </summary>
    public GameActorDamageReportState()
    {
        DamageReports = new();
    }

    /// <summary>
    /// The damage reports.
    /// </summary>
    public DamageReportContainer DamageReports { get; set; }

    /// <summary>
    /// Reset the state.
    /// </summary>
    public void Reset()
    {
        DamageReports.Clear();
    }
}