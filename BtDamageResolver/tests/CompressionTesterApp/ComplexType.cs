using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Tests.CompressionTesterApp;

/// <summary>
/// Complex type for testing.
/// </summary>
public class ComplexType
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
