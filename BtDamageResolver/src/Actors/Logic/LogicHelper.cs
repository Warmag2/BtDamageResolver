using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using Orleans;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    public class LogicHelper
    {
        public IGrainFactory GrainFactory { get; set; }
        
        public IMathExpression MathExpression { get; set; }

        public IResolverRandom Random { get; set; }

        public LogicHelper(
            IGrainFactory grainFactory,
            IMathExpression mathExpression,
            IResolverRandom random)
        {
            GrainFactory = grainFactory;
            MathExpression = mathExpression;
            Random = random;
        }
    }
}
