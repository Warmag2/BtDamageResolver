using System;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver
{
    public static class ExpressionExtensions
    {
        public static bool IsToken(this char input)
        {
            foreach (var token in Enum.GetValues(typeof(Token)).Cast<Token>())
            {
                if (input.Equals((char)token))
                {
                    return true;
                }
            }

            return false;
        }
    }
}