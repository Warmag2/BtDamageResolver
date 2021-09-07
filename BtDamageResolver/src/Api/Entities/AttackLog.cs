using System;
using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class AttackLog
    {
        public AttackLog()
        {
            Log = new List<AttackLogEntry>();
        }

        public List<AttackLogEntry> Log { get; set; }

        public void Append(AttackLogEntry entry)
        {
            Log.Add(entry);
        }

        public void Append(List<AttackLogEntry> logEntries)
        {
            foreach (var logEntry in logEntries)
            {
                Append(logEntry);
            }
        }

        public void Append(AttackLog log)
        {
            Append(log.Log);
        }

        public void Clear()
        {
            Log.Clear();
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Log.Select(l => l.ToString()));
        }
    }
}