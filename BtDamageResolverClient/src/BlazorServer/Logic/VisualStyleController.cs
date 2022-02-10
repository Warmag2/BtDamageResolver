using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    /// <summary>
    /// Controls visual styles and field visibility.
    /// </summary>
    public class VisualStyleController
    {
        /// <summary>
        /// Returns a style text to hide an element or avoid doing so.
        /// </summary>
        /// <param name="hidden">Should the element be hidden.</param>
        /// <returns>A style text which hides the element or nothing, if it should not be hidden.</returns>
        public static string HideElement(bool hidden)
        {
            return hidden ? "display:none" : string.Empty;
        }

        /// <summary>
        /// Indicates whether cover should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if cover should be hidden, <b>false</b> otherwise.</returns>
        public bool GetCoverHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates whether attack direction should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if attack direction should be hidden, <b>false</b> otherwise.</returns>
        public bool GetDirectionHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Building:
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether the number of jump jets should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if the number of jump jets should be hidden, <b>false</b> otherwise.</returns>
        public bool GetJumpJetsHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates whether firing penalty should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if firing penalty should be hidden, <b>false</b> otherwise.</returns>
        public bool GetPenaltyHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether the number of heat sinks should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if the number of heat sinks should be hidden, <b>false</b> otherwise.</returns>
        public bool GetSinksHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.AerospaceFighter:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates whether unit speed should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if unit speed should be hidden, <b>false</b> otherwise.</returns>
        public bool GetSpeedHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Building:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether unit stance should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if unit stance should be hidden, <b>false</b> otherwise.</returns>
        public bool GetStanceHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                case UnitType.Mech:
                case UnitType.MechTripod:
                case UnitType.MechQuad:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates whether unit tonnage should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if unit tonnage should be hidden, <b>false</b> otherwise.</returns>
        public bool GetTonnageHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether trooper count should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if trooper count should be hidden, <b>false</b> otherwise.</returns>
        public bool GetTroopersHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates whether unit state indicators should be hidden for this unit type.
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns><b>True</b> if unit state indicators should be hidden, <b>false</b> otherwise.</returns>
        public bool GetUnitStateHidden(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.BattleArmor:
                case UnitType.Infantry:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets a class for active indication.
        /// </summary>
        /// <param name="active">Active or not.</param>
        /// <returns>The correct class for active indication.</returns>
        public string GetActiveClass(bool active)
        {
            if (active)
            {
                return "active";
            }

            return "inactive";
        }

        /// <summary>
        /// Gets the correct style for a given heat level.
        /// </summary>
        /// <param name="heat">The heat level.</param>
        /// <returns>The correct style for the given heat level.</returns>
        public string GetStyleForHeat(int heat)
        {
            if (heat >= 14)
            {
                return "resolver_status_critical";
            }

            if (heat >= 5)
            {
                return "resolver_status_warning";
            }

            return "resolver_status_normal";
        }

        /// <summary>
        /// Gets the correct style for a given attack penalty.
        /// </summary>
        /// <param name="heat">The attack penalty.</param>
        /// <returns>The correct style for the given attack penalty.</returns>
        public string GetStyleForPenalty(int heat)
        {
            if (heat >= 2)
            {
                return "resolver_status_critical";
            }

            if (heat >= 1)
            {
                return "resolver_status_warning";
            }

            return "resolver_status_normal";
        }
    }
}