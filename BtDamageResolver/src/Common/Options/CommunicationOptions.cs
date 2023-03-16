using System;

namespace Faemiyah.BtDamageResolver.Common.Options;

/// <summary>
/// Defines options for server-client communication.
/// </summary>
[Serializable]
public class CommunicationOptions
{
    /// <summary>
    /// The connection string.
    /// </summary>
    public string ConnectionString { get; set; }
}