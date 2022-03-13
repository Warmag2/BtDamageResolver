using System;
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

        /// <summary>
        /// If the specified dictionary only contains one value with an enum key type,
        /// fill the dictionary with key from the specified enum and the given value.
        /// </summary>
        /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
        /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="acceptNull">If true, and the dictionary is null, this returns null.</param>
        /// <returns>
        /// Null if the dictionary is null and we should accept null,
        /// or a dictionary with the all keys in the enum and either specified values from
        /// the value to fill with or the value already contained in the dictionary.
        /// </returns>
        public static Dictionary<TKey, TValue> Fill<TKey, TValue>(this Dictionary<TKey,TValue> dictionary, bool acceptNull = false)
            where TKey : Enum
        {
            TValue valueToFillWith = default;

            if (dictionary == null)
            {
                if (acceptNull)
                {
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
                    return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
                }

                return Enum.GetValues(typeof(TKey)).Cast<TKey>().ToDictionary(k => k, k => valueToFillWith);
            }

            if (dictionary.Count == 1)
            {
                return Enum.GetValues(typeof(TKey)).Cast<TKey>().ToDictionary(k => k, k => dictionary.Values.Single());
            }

            return dictionary;
        }
    }
}