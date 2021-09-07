using System;
using Faemiyah.BtDamageResolver.Actors.Logic.Interfaces;
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

/*        private decimal SplitIntoExpressions(string expression)
        {
            foreach (var tokenType in new [] {Token.Plus, Token.Minus, Token.Multiply, Token.Divide, Token.Dice})
            {
                if (ContainsToken(expression, tokenType))
                {
                    return CalculateExpression(expression.Split((char)tokenType), tokenType);
                }
            }

            return decimal.Parse(expression);
        }

        private decimal CalculateExpression(string[] expression, Token token)
        {
            // This will fail if we have an expression of type [operator][value] since the lefthand operand is missing. Insert a zero there.
            if(string.IsNullOrWhiteSpace(expression[0]))
            {
                expression[0] = "0";
            }

            if (expression.Length == 1 && !ContainsTokens(expression.Single()))
            {
                return int.Parse(expression.Single());
            }

            switch (token)
            {
                case Token.Plus:
                    return expression.Select(SplitIntoExpressions).Aggregate((total, next) => total + next);
                case Token.Minus:
                    return expression.Select(SplitIntoExpressions).Aggregate((total, next) => total - next);
                case Token.Multiply:
                    return expression.Select(SplitIntoExpressions).Aggregate((total, next) => total * next);
                case Token.Divide:
                    return expression.Select(SplitIntoExpressions).Aggregate((total, next) => total / next);
                case Token.Dice:
                    return expression.Select(SplitIntoExpressions).Aggregate((total, next) =>
                    {
                        var value = 0m;

                        for (var ii = 0; ii < total; ii++)
                        {
                            value += _random.NextPlusOne(next);
                        }

                        return value;
                    });
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }
        }

        private static bool ContainsTokens(string expression)
        {
            return ContainsToken(expression, Token.Dice) || ContainsToken(expression, Token.Multiply) ||
                   ContainsToken(expression, Token.Minus) || ContainsToken(expression, Token.Plus) ||
                   ContainsToken(expression, Token.Divide);
        }

        private static bool ContainsToken(string expression, Token token)
        {
            return expression.Contains((char) token);
        }*/
    }
}