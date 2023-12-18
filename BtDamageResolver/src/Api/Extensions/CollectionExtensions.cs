using System;
using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Extensions;

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
    /// Make a deep copy of a dictionary object.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The type of the item in the <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
    /// <param name="input">The <see cref="Dictionary{TKey, TValue}"/> to make a copy of.</param>
    /// <returns>A deep copy of the input <see cref="Dictionary{TKey, TValue}"/>.</returns>
    public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this Dictionary<TKey, TValue> input)
    {
        var returnValue = new Dictionary<TKey, TValue>();

        foreach (var item in input)
        {
            returnValue.Add(item.Key, item.Value);
        }

        return returnValue;
    }

    /// <summary>
    /// Compare dictionary contents.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The type of the item in the <see cref="Dictionary{TKey, TValue}"/>.</typeparam>
    /// <param name="input">The base <see cref="Dictionary{TKey, TValue}"/>.</param>
    /// <param name="other">The <see cref="Dictionary{TKey, TValue}"/> to compare to.</param>
    /// <returns><b>True</b> if dictionary contents are identical, <b>false</b> otherwise.</returns>
    public static bool DeepEquals<TKey, TValue>(this Dictionary<TKey, TValue> input, Dictionary<TKey, TValue> other)
        where TValue : IComparable<TValue>
    {
        if (input.Count != other.Count)
        {
            return false;
        }

        foreach (var key in input.Keys)
        {
            if (input[key].CompareTo(other[key]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Add the values of a dictionary onto the values of another dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="target">The target dictionary which to add to.</param>
    /// <param name="source">The source dictionary to add from.</param>
    /// <exception cref="InvalidOperationException">If the dictionaries have a different number of elements.</exception>
    public static void MergeAdditionally<TKey>(this Dictionary<TKey, int> target, Dictionary<TKey, int> source)
    {
        if (target.Count != source.Count)
        {
            throw new InvalidOperationException("Dictionaries cannot be merged.");
        }

        foreach (var key in target.Keys)
        {
            target[key] += source[key];
        }
    }

    /// <summary>
    /// Multiply the values of a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="target">The target dictionary which to add to.</param>
    /// <param name="multiple">The multiplication factor.</param>
    /// <exception cref="InvalidOperationException">If the dictionaries have a different number of elements.</exception>
    public static void Multiply<TKey>(this Dictionary<TKey, int> target, int multiple)
    {
        foreach (var key in target.Keys)
        {
            target[key] *= multiple;
        }
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
    /// Increment item with a specific key in dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="countable">The countable element.</param>
    public static void AddIfNotZero<TKey>(this Dictionary<TKey, int> dict, TKey key, int countable)
    {
        if (countable == 0)
        {
            return;
        }

        if (!dict.TryAdd(key, countable))
        {
            dict[key] += countable;
        }
    }

    /// <summary>
    /// Increment item with a specific key in dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="countable">The countable element.</param>
    public static void AddIfNotZero<TKey>(this Dictionary<TKey, double> dict, TKey key, double countable)
    {
        if (countable == 0d)
        {
            return;
        }

        if (!dict.TryAdd(key, countable))
        {
            dict[key] += countable;
        }
    }

    /// <summary>
    /// If and only if the specified dictionary only contains one value with an enum key type,
    /// fill the dictionary with key from the specified enum and the given value until the given bracket.
    /// Fill rest of range brackets with zeroes.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="fillUntil">The range bracket to fill until.</param>
    /// <returns>
    /// The filled dictionary.
    /// </returns>
    public static Dictionary<RangeBracket, int> Fill(this Dictionary<RangeBracket, int> dictionary, RangeBracket fillUntil)
    {
        if (dictionary.Count == 1)
        {
            var valueToFillWith = dictionary.Single().Value;

            var returnValue = new Dictionary<RangeBracket, int>();

            for (int ii = 0; ii <= (int)fillUntil; ii++)
            {
                returnValue[(RangeBracket)ii] = valueToFillWith;
            }

            for (int ii = (int)fillUntil + 1; ii <= (int)RangeBracket.OutOfRange; ii++)
            {
                returnValue[(RangeBracket)ii] = 0;
            }

            return returnValue;
        }

        return dictionary;
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
    public static Dictionary<TKey, TValue> Fill<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, bool acceptNull = false)
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