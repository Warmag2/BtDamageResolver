using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Extensions;

/// <summary>
/// Extensions for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets a list of all values of this enum.
    /// </summary>
    /// <typeparam name="TEnum">The enum to unpack to a list of values.</typeparam>
    /// <returns>A list with all values of the given enum.</returns>
    public static List<TEnum> GetEnumValueList<TEnum>()
        where TEnum : Enum
    {
        var enumValueList = new List<TEnum>();

        foreach (var enumValue in Enum.GetValues(typeof(TEnum)))
        {
            enumValueList.Add((TEnum)enumValue);
        }

        return enumValueList;
    }
}