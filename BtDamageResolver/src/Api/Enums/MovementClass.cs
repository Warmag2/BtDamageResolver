using System;

namespace Faemiyah.BtDamageResolver.Api.Enums;

/// <summary>
/// A movement class.
/// </summary>
[Serializable]
public enum MovementClass
{
    Immobile,
    Stationary,
    Normal,
    Fast,
    Masc,
    Jump,
    OutOfControl
}