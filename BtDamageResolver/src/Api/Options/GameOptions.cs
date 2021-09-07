using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Options
{
    /// <summary>
    /// Contains game-related options.
    /// </summary>
    public class GameOptions
    {
        public GameOptions()
        {
            // Set default rules
            Rules = new Dictionary<Rule, bool> { {Rule.FloatingCritical, true}, {Rule.ImprovedVehicleSurvivability, true} };
        }

        public int PenaltyAll { get; set; }

        public int PenaltyBallistic { get; set; }

        public int PenaltyEnergy { get; set; }
        
        public int PenaltyMissile { get; set; }

        public Dictionary<Rule, bool> Rules { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}