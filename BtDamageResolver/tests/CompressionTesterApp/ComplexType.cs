using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.CompressionTesterApp;

/// <summary>
/// Complex type for testing compression.
/// </summary>
internal sealed class ComplexType
{
    /// <summary>
    /// Guid property.
    /// </summary>
    public Guid Uuid { get; set; }

    /// <summary>
    /// Complex dictionary property.
    /// </summary>
    public Dictionary<string, int> Dict { get; set; }
}