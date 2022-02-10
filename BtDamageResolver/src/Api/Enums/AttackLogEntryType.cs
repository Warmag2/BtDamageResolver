using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// An attack log entry type.
    /// </summary>
    [Serializable]
    public enum AttackLogEntryType
    {
        Calculation,
        Critical,
        Damage,
        DiceRoll,
        Fire,
        Heat,
        Hit,
        Information,
        Miss,
        SpecialDamage
    }
}