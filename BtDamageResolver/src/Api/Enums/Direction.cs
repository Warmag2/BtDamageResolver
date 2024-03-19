using System;
using System.Diagnostics.CodeAnalysis;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Represents the direction of incoming damage, which determines what properties the paperdoll should have.
/// </summary>
[Serializable]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self-evident and fix would be very noisy.")]
public enum Direction
{
    Front,
    Left,
    Right,
    Rear,
    Top,
    Bottom
}