using System;
using Faemiyah.BtDamageResolver.Api;

namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver
{
    public class MathExpression : IMathExpression
    {
        private readonly IResolverRandom _random;

        public MathExpression(IResolverRandom random)
        {
            _random = random;
        }
        
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