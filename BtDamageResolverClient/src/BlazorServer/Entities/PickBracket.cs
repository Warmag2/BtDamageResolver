namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Entities;

/// <summary>
/// Number option picking bracket.
/// </summary>
public class PickBracket
{
    /// <summary>
    /// Beginning of bracket.
    /// </summary>
    public int Begin { get; set; }

    /// <summary>
    /// End of bracket.
    /// </summary>
    public int End { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(false);
    }

    /// <summary>
    /// Produces a string of the PickBracket object with number prefixes if necessary.
    /// </summary>
    /// <param name="displayNumberPrefix">Indicates whether to display number prefixes.</param>
    /// <returns>A string representation of the PickBracket object.</returns>
    public string ToString(bool displayNumberPrefix)
    {
        var begin = displayNumberPrefix && Begin >= 0 ? $"+{Begin}" : $"{Begin}";
        var end = displayNumberPrefix && End >= 0 ? $"+{End}" : $"{End}";

        return Begin != End ? $"{begin}-{end}" : $"{begin}";
    }
}