namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Entities
{
    /// <summary>
    /// Swapping tuple implementation because event callbacks can't take tuples anymore.
    /// </summary>
    public class SwapTuple
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwapTuple"/> class.
        /// </summary>
        /// <param name="from">Where to swap from.</param>
        /// <param name="to">Where to swap to.</param>
        public SwapTuple(int from, int to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Where to swap from.
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// Where to swap to.
        /// </summary>
        public int To { get; set; }
    }
}
