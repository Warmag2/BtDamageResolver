namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;

/// <summary>
/// Parses simple expressions needed for damage calculation.
/// </summary>
public interface IMathExpression
{
    /// <summary>
    /// Parse a mathematical expression.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <returns>The result of the expression.</returns>
    public int Parse(string expression);
}