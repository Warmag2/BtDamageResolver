using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// A special damage type.
    /// </summary>
    [Serializable]
    public enum SpecialDamageType
    {
        None,
        Critical,
        Emp,
        Heat,
        Motive,
        Narc,
        Tag
    }
}
