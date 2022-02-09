using System;

namespace Faemiyah.BtDamageResolver.Api
{
    /// <summary>
    /// A resolver-specific random number generator.
    /// </summary>
    public class ResolverRandom : IResolverRandom
    {
        private readonly Random _rand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverRandom"/> class.
        /// </summary>
        public ResolverRandom()
        {
            _rand = new Random();
        }

        /// <inheritdoc/>
        public int D26()
        {
            return NextPlusOne(6) + NextPlusOne(6);
        }

        /// <inheritdoc/>
        public int Next(int max)
        {
            return _rand.Next(max);
        }

        /// <inheritdoc/>
        public int NextPlusOne(int max)
        {
            return _rand.Next(max) + 1;
        }

        /// <inheritdoc/>
        public int NextPlusOne(decimal max)
        {
            return NextPlusOne(decimal.ToInt32(max));
        }
    }
}