using System;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// The internal state of an unit actor.
    /// </summary>
    [Serializable]
    public class UnitActorState
    {
        public UnitActorState()
        {
            // When an unit is created, set this to zero so that we get all updates
            UpdateTimeStamp = DateTime.MinValue;
        }

        public bool Initialized { get; set; }

        public UnitEntry UnitEntry { get; set; }

        public DateTime UpdateTimeStamp { get; set; }
    }
}