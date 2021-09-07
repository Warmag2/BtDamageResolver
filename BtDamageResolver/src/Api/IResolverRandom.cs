namespace Faemiyah.BtDamageResolver.Api
{
    public interface IResolverRandom
    {
        public int D26();

        public int Next(int max);
        
        public int NextPlusOne(int max);

        public int NextPlusOne(decimal max);
    }
}