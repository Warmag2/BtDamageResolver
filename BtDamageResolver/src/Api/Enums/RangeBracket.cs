using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// A range bracket.
    /// </summary>
    [Serializable]
    public enum RangeBracket
    {
        PointBlank,
        Short,
        Medium,
        Long,
        Extreme,
        OutOfRange
    }
}