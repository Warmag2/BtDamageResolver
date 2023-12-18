namespace Faemiyah.BtDamageResolver.Api.Constants;

/// <summary>
/// Contains common constants for resolver usage.
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

    /// <summary>
    /// Battle armor weapon names start with this string.
    /// </summary>
    public const string BattleArmorWeaponPrefix = "BA ";

    /// <summary>
    /// Infantry weapon names start with this string.
    /// </summary>
    public const string InfantryWeaponPrefix = "Infantry ";
}