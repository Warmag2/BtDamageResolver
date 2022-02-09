using System;
using Faemiyah.BtDamageResolver.Api;

namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver
{
    /// <summary>
    /// The math expression solver.
    /// </summary>
    public class MathExpression : IMathExpression
    {
        private readonly IResolverRandom _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="MathExpression"/> class.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        public MathExpression(IResolverRandom random)
        {
            _random = random;
        }

        /// <inheritdoc/>
        public int Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return 0;
            }

            var expressionObject = new Expression(_random, expression);

            return decimal.ToInt32(Math.Round(expressionObject.Parse(), MidpointRounding.AwayFromZero));
        }
    }
}