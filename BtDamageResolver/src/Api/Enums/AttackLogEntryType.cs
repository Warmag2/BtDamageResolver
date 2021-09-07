using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
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