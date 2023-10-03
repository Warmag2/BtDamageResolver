using Faemiyah.BtDamageResolver.Api.Enums;
using Faemiyah.BtDamageResolver.Api.Validation;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// Validation methods and helper methods for unit entries.
/// </summary>
public partial class UnitEntry
{
    /// <inheritdoc />
    public RulesValidationResult Validate()
    {
        // Make a new validation result and go over error cases one by one
        var validationResult = new RulesValidationResult();

        // Movement
        switch (Type)
        {
            case UnitType.Building:
                if (MovementClass != MovementClass.Immobile)
                {
                    validationResult.Fail("Unit is a building, but its movement is not Immobile-0.");
                }

                break;
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropship:
            case UnitType.AerospaceFighter:
                switch (MovementClass)
                {
                    case MovementClass.Masc:
                    case MovementClass.Jump:
                        validationResult.Fail("Masc/Jump are invalid movement modes for aerospace units.");
                        break;
                }

                break;
            case UnitType.BattleArmor:
            case UnitType.Infantry:
                switch (MovementClass)
                {
                    case MovementClass.Fast:
                    case MovementClass.Masc:
                    case MovementClass.OutOfControl:
                        validationResult.Fail("Fast/Masc/OutOfControl are invalid movement modes for battle armor and infantry units.");
                        break;
                }

                break;
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                switch (MovementClass)
                {
                    case MovementClass.OutOfControl:
                        validationResult.Fail("OutOfControl is an invalid movement mode for a mech unit.");
                        break;
                }

                break;
            case UnitType.VehicleHover:
            case UnitType.VehicleTracked:
            case UnitType.VehicleVtol:
            case UnitType.VehicleWheeled:
                switch (MovementClass)
                {
                    case MovementClass.Masc:
                    case MovementClass.Jump:
                    case MovementClass.OutOfControl:
                        validationResult.Fail("Masc/Jump/OutOfControl are invalid movement modes for vehicles.");
                        break;
                }

                break;
        }

        if (MovementClass == MovementClass.Masc && !HasFeature(UnitFeature.Masc))
        {
            validationResult.Fail("Unit has Masc movement mode but no MASC feature.");
        }

        if (Movement < 0)
        {
            validationResult.Fail("Negative movement amount is not allowed.");
        }

        switch (MovementClass)
        {
            case MovementClass.Immobile:
            case MovementClass.Stationary:
                if (Movement > 0)
                {
                    validationResult.Fail("Immobile/Stationary only allow movement amount 0.");
                }

                break;
            case MovementClass.Normal:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail("Normal movement only allows movement up to speed.");
                }

                break;
            case MovementClass.Fast:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail("Fast movement only allows movement up to 1.5*speed.");
                }

                break;
            case MovementClass.Masc:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail("Masc movement only allows movement up to 2*speed.");
                }

                break;
            case MovementClass.Jump:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail("Jump movement only allows movement up to the amount of jump jets.");
                }

                break;
            case MovementClass.OutOfControl:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail("Out of control movement only allows movement up to 1.5*speed.");
                }

                break;
        }

        // Trooper amount
        switch (Type)
        {
            case UnitType.BattleArmor:
            case UnitType.Infantry:
                if (Troopers is < 1 or > 30)
                {
                    validationResult.Fail("Squads can have between 1 and 30 troopers.");
                }

                break;
        }

        // Weapon load
        switch (Type)
        {
            case UnitType.BattleArmor:
                if (Weapons.Exists(w => !w.IsBattleArmorWeapon()))
                {
                    validationResult.Fail("Battle armor unit has weapons which are not battle armor weapons.");
                }

                break;
            case UnitType.Infantry:
                if (Weapons.Exists(w => !w.IsInfantryWeapon()))
                {
                    validationResult.Fail("Infantry unit has weapons which are not infantry weapons.");
                }

                break;
            default:
                if (Weapons.Exists(w => w.IsBattleArmorWeapon() || w.IsInfantryWeapon()))
                {
                    validationResult.Fail("Non-infantry non-battlearmor unit has weapons which are either infantry or battle armor weapons.");
                }

                break;
        }

        // Tonnage
        if (Tonnage < 0)
        {
            validationResult.Fail("Negative tonnage is not valid.");
        }

        switch (Type)
        {
            // Aerospace capital ships do not have a relevant maximum tonnage
            case UnitType.AerospaceDropship:
                if (Tonnage is < 100 or > 100000)
                {
                    validationResult.Fail("Dropship/Small Craft minimum tonnage is 100 tons and maximum tonnage is 100000 tons.");
                }

                break;
            case UnitType.AerospaceFighter:
                if (Tonnage is < 5 or > 100)
                {
                    validationResult.Fail("Aerospace fighter minimum tonnage is 5 tons and maximum tonnage is 100 tons.");
                }

                break;
            case UnitType.Mech:
            case UnitType.MechTripod:
            case UnitType.MechQuad:
                if (Tonnage is < 20 or > 100)
                {
                    validationResult.Fail("Mech minimum tonnage is 20 tons and maximum tonnage is 100 tons.");
                }

                break;
            case UnitType.VehicleHover:
                if (Tonnage > 50)
                {
                    validationResult.Fail("Hover vehicle maximum tonnage is 50 tons.");
                }

                break;
            case UnitType.VehicleTracked:
                if (Tonnage > 200)
                {
                    validationResult.Fail("(Superheavy) Tracked vehicle maximum tonnage is 200 tons.");
                }

                break;
            case UnitType.VehicleVtol:
                if (Tonnage > 30)
                {
                    validationResult.Fail("VTOL vehicle maximum tonnage is 30 tons.");
                }

                break;
            case UnitType.VehicleWheeled:
                if (Tonnage > 160)
                {
                    validationResult.Fail("(Superheavy) Wheeled vehicle maximum tonnage is 160 tons.");
                }

                break;
        }

        // Features
        foreach (var feature in Features)
        {
            switch (feature)
            {
                case UnitFeature.ArmorHeatResistant:
                    switch (Type)
                    {
                        case UnitType.BattleArmor:
                            break;
                        default:
                            validationResult.Fail($"{feature} is not valid for non-battlearmor units.");
                            break;
                    }

                    break;
                case UnitFeature.BattleFists:
                case UnitFeature.CommandUnit:
                case UnitFeature.Cowl:
                case UnitFeature.ExtendedTorsoTwist:
                case UnitFeature.JettisonableWeapon:
                case UnitFeature.Masc:
                case UnitFeature.NoMinimalArms:
                case UnitFeature.ProtectedActuators:
                case UnitFeature.ReinforcedLegs:
                case UnitFeature.Stable:
                case UnitFeature.Unbalanced:
                case UnitFeature.WeakLegs:
                    switch (Type)
                    {
                        case UnitType.Mech:
                        case UnitType.MechTripod:
                        case UnitType.MechQuad:
                            break;
                        default:
                            validationResult.Fail($"{feature} is only valid for mech units.");
                            break;
                    }

                    break;
                case UnitFeature.CombatComputer:
                    switch (Type)
                    {
                        case UnitType.AerospaceFighter:
                        case UnitType.Mech:
                        case UnitType.MechTripod:
                        case UnitType.MechQuad:
                            break;
                        default:
                            validationResult.Fail($"{feature} is only valid for units which track heat.");
                            break;
                    }

                    break;
                case UnitFeature.NimbleJumper:
                    switch (Type)
                    {
                        case UnitType.BattleArmor:
                        case UnitType.Infantry:
                        case UnitType.Mech:
                        case UnitType.MechTripod:
                        case UnitType.MechQuad:
                            break;
                        default:
                            validationResult.Fail($"{feature} is not valid for non-mech, non-infantry units.");
                            break;
                    }

                    break;
                case UnitFeature.Ams:
                case UnitFeature.EasyToPilot:
                case UnitFeature.HardToPilot:
                case UnitFeature.PoorPerformance:
                case UnitFeature.NarrowLowProfile:
                case UnitFeature.StabilizedWeapons:
                    switch (Type)
                    {
                        case UnitType.BattleArmor:
                        case UnitType.Infantry:
                            validationResult.Fail($"{feature} is not valid for infantry units.");
                            break;
                        default:
                            break;
                    }

                    break;
            }
        }

        return validationResult;
    }
}
