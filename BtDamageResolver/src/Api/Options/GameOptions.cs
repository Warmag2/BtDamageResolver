using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Options;

/// <summary>
/// Contains game-related options.
/// </summary>
public class GameOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameOptions"/> class.
    /// </summary>
    public GameOptions()
    {
        // Set default rules
        Rules = new Dictionary<Rule, bool> { { Rule.FloatingCritical, true }, { Rule.ImprovedVehicleSurvivability, true } };
    }

    /// <summary>
    /// The penalty for all weapons fire.
    /// </summary>
    public int PenaltyAll { get; set; }

    /// <summary>
    /// Penalty for ballistic weapon fire.
    /// </summary>
    public int PenaltyBallistic { get; set; }

    /// <summary>
    /// Penalty for energy weapon fire.
    /// </summary>
    public int PenaltyEnergy { get; set; }

    /// <summary>
    /// Penalty for missile weapon fire.
    /// </summary>
    public int PenaltyMissile { get; set; }

    /// <summary>
    /// All active rules.
    /// </summary>
    public Dictionary<Rule, bool> Rules { get; set; }

    /// <summary>
    /// The modification timestamp.
    /// </summary>
    public DateTime TimeStamp { get; set; }
}