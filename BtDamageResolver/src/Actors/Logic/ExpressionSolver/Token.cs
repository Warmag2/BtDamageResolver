namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;

/// <summary>
/// List of expression tokens.
/// </summary>
/// <remarks>These have to be in resolution order.</remarks>
public enum Token
{
    /// <summary>
    /// Dice roll.
    /// </summary>
    Dice = 'd',

    /// <summary>
    /// Exponent.
    /// </summary>
    Exponent = '^',

    /// <summary>
    /// Division.
    /// </summary>
    Divide = '/',

    /// <summary>
    /// Multiplication.
    /// </summary>
    Multiply = '*',

    /// <summary>
    /// Addition.
    /// </summary>
    Plus = '+',

    /// <summary>
    /// Subtraction.
    /// </summary>
    Minus = '-'
}