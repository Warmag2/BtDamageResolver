using System;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class Ping
    {
        public Ping()
        {
            TimeStamp = DateTime.UtcNow;
        }

        public DateTime TimeStamp { get; set; }
    }
}