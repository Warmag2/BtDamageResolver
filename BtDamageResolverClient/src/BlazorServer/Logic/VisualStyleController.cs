using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class VisualStyleController
    {
        public static string HideElement(bool hidden)
        {
            return hidden ? "display:none" : string.Empty;
        }

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

        public string GetActiveClass(bool active)
        {
            if (active)
            {
                return "active";
            }

            return "inactive";
        }

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