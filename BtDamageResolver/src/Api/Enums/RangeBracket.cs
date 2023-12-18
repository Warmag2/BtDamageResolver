using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// A range bracket.
/// </summary>
[Serializable]
public enum RangeBracket
{
    /// <summary>
    /// Same hex.
    /// </summary>
    PointBlank = 0,

    /// <summary>
    /// Short range bracket.
    /// </summary>
    Short = 1,

    /// <summary>
    /// Medium range bracket.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Long range bracket.
    /// </summary>
    Long = 3,

    /// <summary>
    /// Extreme range bracket.
    /// </summary>
    Extreme = 4,

    /// <summary>
    /// Out of weapon firing range.
    /// </summary>
    OutOfRange = 5
}