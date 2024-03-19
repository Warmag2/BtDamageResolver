using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Cover represents the cover of an unit, from its targets point of view.
/// </summary>
[Serializable]
public enum Cover
{
    /// <summary>
    /// No cover.
    /// </summary>
    None,

    /// <summary>
    /// Lower cover.
    /// </summary>
    Lower,

    /// <summary>
    /// Left side cover (left torso + left leg + left arm).
    /// </summary>
    Left,

    /// <summary>
    /// Right side cover (right torso + right leg + right arm).
    /// </summary>
    Right,

    /// <summary>
    /// Upper cover (arms + head).
    /// </summary>
    Upper,
}