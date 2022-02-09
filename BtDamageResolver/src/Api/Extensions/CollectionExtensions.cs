using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    /// <summary>
    /// Extensions for collection types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Make a deep copy of a hashset object.
        /// </summary>
        /// <typeparam name="TType">The type of the item in the <see cref="HashSet{TType}"/>.</typeparam>
        /// <param name="input">The <see cref="HashSet{TType}"/> to make a copy of.</param>
        /// <returns>A deep copy of the input <see cref="HashSet{TType}"/>.</returns>
        public static HashSet<TType> Copy<TType>(this HashSet<TType> input)
        {
            var returnValue = new HashSet<TType>();

            foreach (var item in input)
            {
                returnValue.Add(item);
            }

            return returnValue;
        }

        /// <summary>
        /// Alter the state of a hash set in a way that an item will be present or not present in the in the hash set.
        /// </summary>
        /// <typeparam name="TType">The type of the item in the <see cref="HashSet{TType}"/>.</typeparam>
        /// <param name="input">The <see cref="HashSet{TType}"/> to make a copy of.</param>
        /// <param name="item">The item whose presence in the hash set to alter.</param>
        /// <param name="present">Should the item be present in the hash set or not.</param>
        public static void Set<TType>(this HashSet<TType> input, TType item, bool present)
        {
            if (present)
            {
                input.Add(item);
            }
            else
            {
                input.Remove(item);
            }
        }

        /// <summary>
        /// Add the item to the list only if it is not null.
        /// </summary>
        /// <typeparam name="TType">The type of the item in the <see cref="List{TType}"/>.</typeparam>
        /// <param name="inputList">The list to append to.</param>
        /// <param name="input">The item to check for addition.</param>
        public static void AddIfNotNull<TType>(this List<TType> inputList, TType input)
            where TType : class
        {
            if (input != null)
            {
                inputList.Add(input);
            }
        }
    }
}