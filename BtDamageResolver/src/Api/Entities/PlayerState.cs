using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class PlayerState
    {
        public PlayerState()
        {
            TimeStamp = DateTime.UtcNow;
        }

        public string PlayerId { get; set; }

        public DateTime TimeStamp { get; set; }

        public bool IsReady { get; set; }

        public List<UnitEntry> UnitEntries { get; set; }
    }
}
