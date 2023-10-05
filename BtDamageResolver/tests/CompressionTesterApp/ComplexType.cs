﻿using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Complex type for testing compression.
/// </summary>
public class ComplexType
{
    /// <summary>
    /// Guid property.
    /// </summary>
    public Guid Uuid { get; set; }

    /// <summary>
    /// Complex property.
    /// </summary>
    public Dictionary<string, int> Dict { get; set; }
}