using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class EnumExtensions
    {
        public static bool CanTakeMotiveHits(this UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.VehicleHover:
                case UnitType.VehicleTracked:
                case UnitType.VehicleVtol:
                case UnitType.VehicleWheeled:
                    return true;
                default:
                    return false;
            }
        }
    }
}