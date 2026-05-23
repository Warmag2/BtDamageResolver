using System.Numerics;

namespace Faemiyah.BtDamageResolver.Api.Extensions;

/// <summary>
/// Extensions for number values.
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Produce a string representation with an explicit + or - sign for the given number.
    /// </summary>
    /// <typeparam name="TValue">The type of the number.</typeparam>
    /// <returns>A string representation of the integer with an explicit sign.</returns>
    public static string ToStringExplicit<TValue>(this TValue value)
        where TValue : INumber<TValue>
    {
        return value >= TValue.Zero ? "+" + value.ToString() : value.ToString();
    }
}