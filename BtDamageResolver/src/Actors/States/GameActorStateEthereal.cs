using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// Represents those aspects of a game actor which are not persisted to disk.
    /// </summary>
    [Serializable]
    public class GameActorStateEthereal
    {
        public GameActorStateEthereal()
        {
            Reset();
        }

        public List<DamageReport> DamageReports { get; set; }

        public int Turn { get; set; }

        public DateTime TurnTimeStamp { get; set; }

        public void Reset()
        {
            DamageReports = new List<DamageReport>();
            TurnTimeStamp = DateTime.UtcNow;
            Turn = 0;
        }
    }
}
