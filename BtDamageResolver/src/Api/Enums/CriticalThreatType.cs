using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// The type of the threat which induced a critical damage.
    /// </summary>
    [Serializable]
    public enum CriticalThreatType
    {
        DamageThreshold,
        Normal
    }
}