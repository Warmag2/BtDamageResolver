namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;

/// <summary>
/// The function to apply to an expression.
/// </summary>
public enum ExpressionFunction
{
    /// <summary>
    /// No effect.
    /// </summary>
    None,

    /// <summary>
    /// Round.
    /// </summary>
    Round,

    /// <summary>
    /// Ceiling.
    /// </summary>
    Ceil,

    /// <summary>
    /// Floor.
    /// </summary>
    Floor
}