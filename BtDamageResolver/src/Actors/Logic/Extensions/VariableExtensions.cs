using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Extensions;

/// <summary>
/// Extensions for variable insertion.
/// </summary>
public static class VariableExtensions
{
    /// <summary>
    /// Insert variables from an unit entry to a math expression string.
    /// </summary>
    /// <param name="input">The expression string to insert to.</param>
    /// <param name="unit">The unit to insert from.</param>
    /// <param name="weaponBay">The bay to insert from.</param>
    /// <returns>A string with the variables replaced.</returns>
    public static string InsertVariables(this string input, FiringSolution firingSolution, UnitEntry unit)
    {
        return input
            .Replace(Names.ExpressionVariableNameDistance, firingSolution.Distance.ToString())
            .Replace(Names.ExpressionVariableNameTonnage, unit.Tonnage.ToString());
    }
}