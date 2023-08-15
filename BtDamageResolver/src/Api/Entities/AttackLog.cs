using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// The attack log.
/// </summary>
[Serializable]
public class AttackLog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttackLog"/> class.
    /// </summary>
    public AttackLog()
    {
        Log = new List<AttackLogEntry>();
    }

    /// <summary>
    /// The log.
    /// </summary>
    public List<AttackLogEntry> Log { get; set; }

    /// <summary>
    /// Append an item to the attack log.
    /// </summary>
    /// <param name="entry">The entry to append.</param>
    public void Append(AttackLogEntry entry)
    {
        Log.Add(entry);
    }

    /// <summary>
    /// Append multiple entries to the attack log.
    /// </summary>
    /// <param name="logEntries">The entries to append.</param>
    public void Append(List<AttackLogEntry> logEntries)
    {
        foreach (var logEntry in logEntries)
        {
            Append(logEntry);
        }
    }

    /// <summary>
    /// Append another attack log to this log.
    /// </summary>
    /// <param name="log">The log to append.</param>
    public void Append(AttackLog log)
    {
        Append(log.Log);
    }
}