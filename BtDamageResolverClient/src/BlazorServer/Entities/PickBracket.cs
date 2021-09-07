namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Entities
{
    public class PickBracket
    {
        public int Begin { get; set; }

        public int End { get; set; }

        public override string ToString()
        {
            return Begin != End ? $"{Begin}-{End}" : $"{Begin}";
        }
    }
}