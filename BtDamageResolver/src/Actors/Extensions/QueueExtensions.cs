using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Actors.Extensions
{
    public static class QueueExtensions
    {
        public static List<TType> DumpIntoList<TType>(this Queue<TType> queue)
        {
            var list = new List<TType>();
            
            while(queue.Count > 0)
            {
                list.Add(queue.Dequeue());
            }

            return list;
        }
    }
}