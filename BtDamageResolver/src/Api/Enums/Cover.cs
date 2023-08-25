using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// Cover represents the cover of an unit, from its targets point of view.
/// </summary>
[Serializable]
public enum Cover
{
    None,
    Lower,
    Left,
    Right,
    Upper,
}