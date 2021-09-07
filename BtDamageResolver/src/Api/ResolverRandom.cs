using System;

namespace Faemiyah.BtDamageResolver.Api
{
    public class ResolverRandom : IResolverRandom
    {
        private readonly Random _rand;

        public ResolverRandom()
        {
            _rand = new Random();
        }

        public int D26()
        {
            return NextPlusOne(6) + NextPlusOne(6);
        }

        public int Next(int max)
        {
            return _rand.Next(max);
        }

        public int NextPlusOne(int max)
        {
            return _rand.Next(max) + 1;
        }

        public int NextPlusOne(decimal max)
        {
            return _rand.Next(decimal.ToInt32(max)) + 1;
        }
    }
}