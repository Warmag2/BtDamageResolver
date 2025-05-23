using System.Linq;
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

        ValidateFeatures(validationResult);
        ValidateMovement(validationResult);
        ValidateMovementClass(validationResult);
        ValidateTonnage(validationResult);
        ValidateTrooperAmount(validationResult);
        ValidateWeapons(validationResult);

        return validationResult;
    }

    private void ValidateFeatures(RulesValidationResult validationResult)
    {
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
                case UnitFeature.DirectionalTorsoMount:
                case UnitFeature.ExposedActuators:
                case UnitFeature.ExtendedTorsoTwist:
                case UnitFeature.HyperExtendingActuators:
                case UnitFeature.JettisonableWeapon:
                case UnitFeature.Masc:
                case UnitFeature.NoMinimalArms:
                case UnitFeature.ProtectedActuators:
                case UnitFeature.ReinforcedLegs:
                case UnitFeature.Stable:
                case UnitFeature.Unbalanced:
                case UnitFeature.WeakHeadArmor:
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
                case UnitFeature.MultiTarget:
                case UnitFeature.TargetingComputer:
                    switch (Type)
                    {
                        case UnitType.AerospaceCapital:
                        case UnitType.AerospaceDropshipAerodyne:
                        case UnitType.AerospaceDropshipSpheroid:
                        case UnitType.BattleArmor:
                        case UnitType.Infantry:
                            validationResult.Fail($"{feature} is only valid for units which only have a single targeting entity.");
                            break;
                        default:
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
                case UnitFeature.ExposedWeaponLinkage:
                case UnitFeature.FastReload:
                case UnitFeature.HardToPilot:
                case UnitFeature.NarrowLowProfile:
                case UnitFeature.PoorPerformance:
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
    }

    private void ValidateMovement(RulesValidationResult validationResult)
    {
        if (MovementClass == MovementClass.Masc && !HasFeature(UnitFeature.Masc))
        {
            validationResult.Fail("Unit has Masc movement mode but no MASC feature.");
        }

        if (Movement < 0)
        {
            validationResult.Fail("Negative movement amount is not allowed.");
        }

        // Speed
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
                    validationResult.Fail($"With penalty {GetHeatSpeedPenalty()}, Normal movement only allows movement up to speed {GetCurrentSpeed()}.");
                }

                break;
            case MovementClass.Fast:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail($"With penalty {GetHeatSpeedPenalty()}, Fast movement only allows movement up to {GetCurrentSpeed()}.");
                }

                break;
            case MovementClass.Masc:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail($"With penalty {GetHeatSpeedPenalty()}, Masc movement only allows movement up to {GetCurrentSpeed()}.");
                }

                break;
            case MovementClass.Jump:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail($"Jump movement only allows movement up to the amount of jump jets ({GetCurrentSpeed()}).");
                }

                break;
            case MovementClass.OutOfControl:
                if (Movement > GetCurrentSpeed())
                {
                    validationResult.Fail($"With penalty {GetHeatSpeedPenalty()}, Out of control movement only allows movement up to {GetCurrentSpeed()}.");
                }

                break;
        }
    }

    private void ValidateMovementClass(RulesValidationResult validationResult)
    {
        switch (Type)
        {
            case UnitType.Building:
                if (MovementClass != MovementClass.Immobile)
                {
                    validationResult.Fail("Unit is a building, but its movement is not Immobile-0.");
                }

                break;
            case UnitType.AerospaceCapital:
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
            case UnitType.AerospaceFighter:
                switch (MovementClass)
                {
                    case MovementClass.Masc:
                    case MovementClass.Jump:
                        validationResult.Fail("Masc/Jump are invalid movement modes for aerospace units.");
                        break;
                }

                if (Evading)
                {
                    switch (MovementClass)
                    {
                        case MovementClass.Immobile:
                        case MovementClass.Stationary:
                        case MovementClass.OutOfControl:
                            validationResult.Fail("Immobile, Stationary or OutOfControl units cannot evade.");
                            break;
                    }
                }

                break;
            case UnitType.BattleArmor:
            case UnitType.Infantry:
                switch (MovementClass)
                {
                    case MovementClass.Immobile:
                    case MovementClass.Stationary:
                    case MovementClass.Masc:
                    case MovementClass.Fast:
                    case MovementClass.OutOfControl:
                        validationResult.Fail("Immobile/Stationary/Fast/Masc/OutOfControl are invalid movement modes for battle armor and infantry units.");
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
    }

    private void ValidateTonnage(RulesValidationResult validationResult)
    {
        if (Tonnage < 0)
        {
            validationResult.Fail("Negative tonnage is not valid.");
        }

        switch (Type)
        {
            // Aerospace capital ships do not have a relevant maximum tonnage
            case UnitType.AerospaceCapital:
                if (Tonnage < 100000)
                {
                    validationResult.Fail("Capital ship minimum tonnage is 100000 tons.");
                }

                break;
            case UnitType.AerospaceDropshipAerodyne:
            case UnitType.AerospaceDropshipSpheroid:
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
                if (Tonnage > 100)
                {
                    validationResult.Fail("(Superheavy) Hover vehicle maximum tonnage is 100 tons.");
                }

                break;
            case UnitType.VehicleTracked:
                if (Tonnage > 200)
                {
                    validationResult.Fail("(Superheavy) Tracked vehicle maximum tonnage is 200 tons.");
                }

                break;
            case UnitType.VehicleVtol:
                if (Tonnage > 60)
                {
                    validationResult.Fail("(Superheavy) VTOL vehicle maximum tonnage is 60 tons.");
                }

                break;
            case UnitType.VehicleWheeled:
                if (Tonnage > 160)
                {
                    validationResult.Fail("(Superheavy) Wheeled vehicle maximum tonnage is 160 tons.");
                }

                break;
        }
    }

    private void ValidateTrooperAmount(RulesValidationResult validationResult)
    {
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
    }

    private void ValidateWeapons(RulesValidationResult validationResult)
    {
        foreach (var weaponEntry in WeaponBays.SelectMany(w => w.Weapons))
        {
            if (!CanMountWeapon(weaponEntry))
            {
                validationResult.Fail($"Unit of type {Type} can not mount weapon {weaponEntry.WeaponName}.");
            }

            if (weaponEntry.Amount is < 1 or > 12)
            {
                validationResult.Fail($"Bay can contain from 1 to 12 weapons of the same type. Offending weapon: {weaponEntry.WeaponName}.");
            }

            // Weapon multiplication per unit type
            switch (Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropshipAerodyne:
                case UnitType.AerospaceDropshipSpheroid:
                case UnitType.AerospaceFighter:
                    break;
                default:
                    if (weaponEntry.Amount > 1)
                    {
                        validationResult.Fail($"Non-aerospace units cannot have multiplied weapons. Offending weapon: {weaponEntry.WeaponName}.");
                    }

                    break;
            }
        }

        // Weapon bay mergers for capital aerospace when a bay has more than 1 weapon
        foreach (var weaponBay in WeaponBays.Where(w => w.Weapons.Count > 1))
        {
            var ammoNameToMatch = weaponBay.Weapons[0].Ammo;

            switch (Type)
            {
                case UnitType.AerospaceCapital:
                case UnitType.AerospaceDropshipAerodyne:
                case UnitType.AerospaceDropshipSpheroid:
                    if (weaponBay.Weapons.Exists(w => w.Ammo != ammoNameToMatch))
                    {
                        validationResult.Fail($"Unit of type {Type} has weapons with differing ammo in its bay. Offending bay: {weaponBay.Name}.");
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
