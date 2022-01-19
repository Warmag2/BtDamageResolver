namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver
{
    /// <summary>
    /// Parses simple expressions needed for damage calculation.
    /// </summary>
    public interface IMathExpression
    {
        public int Parse(string expression);
    }
}