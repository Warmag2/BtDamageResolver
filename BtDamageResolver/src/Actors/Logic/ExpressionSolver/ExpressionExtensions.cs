using System;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;

/// <summary>
/// Expression extensions.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Is the given character a token.
    /// </summary>
    /// <param name="input">The character to test.</param>
    /// <returns><b>True</b> if the character is a token, <b>false</b> otherwise.</returns>
    public static bool IsToken(this char input)
    {
        if (Enum.GetValues(typeof(Token)).Cast<Token>().Any(t => input.Equals((char)t)))
        {
            return true;
        }

        return false;
    }
}