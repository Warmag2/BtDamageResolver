using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Extensions
{
    public static class ListExtensions
    {
        public static bool MoveUp<T>(this IList<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index <= 0) return false;
            (list[index - 1], list[index]) = (list[index], list[index - 1]);
            return true;
        }

        public static bool MoveDown<T>(this IList<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index < 0 || index >= list.Count - 1) return false;
            (list[index], list[index + 1]) = (list[index + 1], list[index]);
            return true;
        }
    }
}
