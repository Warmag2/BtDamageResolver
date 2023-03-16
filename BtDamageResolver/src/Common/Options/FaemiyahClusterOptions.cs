using System;

namespace Faemiyah.BtDamageResolver.Common.Options;

/// <summary>
/// Defines options for Orleans clustering.
/// </summary>
[Serializable]
public class FaemiyahClusterOptions
{
    /// <summary>
    /// The connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// The invariant of AdoNet clustering.
    /// </summary>
    public string Invariant { get; set; }
}