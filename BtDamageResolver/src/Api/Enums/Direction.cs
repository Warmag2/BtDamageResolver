using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// Represents the direction of incoming damage, which determines what properties the paperdoll should have.
    /// </summary>
    [Serializable]
    public enum Direction
    {
        Front,
        Left,
        Right,
        Rear,
        Top,
        Bottom
    }
}
