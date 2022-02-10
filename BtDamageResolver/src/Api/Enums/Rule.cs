namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// An enum for any optional toggleable rules.
    /// </summary>
    public enum Rule
    {
        /// <summary>
        /// Critical hits may occur on any part of a mech when hit, not just the 7-location. TacOps. p.77.
        /// </summary>
        FloatingCritical,

        /// <summary>
        /// Vehicles have improved survivability and take less crits and motive hits. TacOps p.107.
        /// </summary>
        ImprovedVehicleSurvivability,
    }
}