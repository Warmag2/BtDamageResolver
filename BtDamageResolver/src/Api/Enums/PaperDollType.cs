using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// Represents different paper doll types in the game.
    /// <remarks>There are less of these than real Battletech unit types. Unit types are mapped into paper doll types.</remarks>
    /// </summary>
    [Serializable]
    public enum PaperDollType
    {
        AerospaceFighter,
        AerospaceCapital,
        BattleArmor,
        Building,
        Mech,
        MechTripod,
        MechQuad,
        Trooper,
        Vehicle,
        VehicleVtol
    }
}