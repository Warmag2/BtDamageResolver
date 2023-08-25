namespace Faemiyah.BtDamageResolver.Api.Constants;

/// <summary>
/// Contains common constants for Orleans usage.
/// </summary>
public static class Names
{
    /// <summary>
    /// Name of the data store for actor states.
    /// </summary>
    public const string DefaultClusterTableName = "Default";

    /// <summary>
    /// Distance variable replacement.
    /// </summary>
    public const string ExpressionVariableNameDistance = "$distance";

    /// <summary>
    /// Tonnage variable replacement.
    /// </summary>
    public const string ExpressionVariableNameTonnage = "$tonnage";
}