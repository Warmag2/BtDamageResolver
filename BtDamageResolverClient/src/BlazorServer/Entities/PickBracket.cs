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
        return Begin != End ? $"{Begin}-{End}" : $"{Begin}";
    }
}