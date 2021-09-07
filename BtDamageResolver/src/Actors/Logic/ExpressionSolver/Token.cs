namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver
{
    /// <summary>
    /// List of expression tokens.
    /// </summary>
    /// <remarks>These have to be in resolution order.</remarks>
    public enum Token
    {
        Dice = 'd',
        Exponent = '^',
        Divide = '/',
        Multiply = '*',
        Plus = '+',
        Minus = '-'
    }
}