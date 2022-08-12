using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Actors.States
{
    /// <summary>
    /// A two-way map which maps an item to another item.
    /// </summary>
    /// <remarks>
    /// Not thread-safe. User must keep track of thread safety.
    /// </remarks>
    /// <typeparam name="TType1">The type to map from.</typeparam>
    /// <typeparam name="TType2">The type to map to.</typeparam>
    public class TwoWayMap<TType1, TType2>
        where TType1 : IComparable<TType1>
        where TType2 : IComparable<TType2>
    {
        private readonly Dictionary<TType1, TType2> _mapForward;
        private readonly Dictionary<TType2, TType1> _mapBackward;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoWayMap{TType1, TType2}"/> class.
        /// </summary>
        public TwoWayMap()
        {
            _mapForward = new Dictionary<TType1, TType2>();
            _mapBackward = new Dictionary<TType2, TType1>();
        }

        /// <summary>
        /// Add a key-value pair to the map.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value for the key.</param>
        /// <returns><b>True</b> if adding the key-value pair was successful, and <b>false</b> if either the key or the value already existed in the map.</returns>
        public bool Add(TType1 key, TType2 value)
        {
            if (_mapForward.ContainsKey(key) || _mapBackward.ContainsKey(value))
            {
                return false;
            }

            _mapForward.Add(key, value);
            _mapBackward.Add(value, key);

            return true;
        }

        /// <summary>
        /// Delete a key.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns><b>True</b> if the key was deleted, <b>false</b> if it was not found in the map.</returns>
        public bool Delete(TType1 key)
        {
            if (_mapForward.TryGetValue(key, out var value))
            {
                _mapForward.Remove(key);
                _mapBackward.Remove(value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Delete a value.
        /// </summary>
        /// <param name="value">The value to delete.</param>
        /// <returns><b>True</b> if the value was deleted, <b>false</b> if it was not found in the map.</returns>
        public bool Delete(TType2 value)
        {
            if (_mapBackward.TryGetValue(value, out var key))
            {
                _mapForward.Remove(key);
                _mapBackward.Remove(value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a value for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value for the key.</returns>
        public TType2 Get(TType1 key)
        {
            if (_mapForward.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        /// <summary>
        /// Get a key for a value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The key for the value.</returns>
        public TType1 Get(TType2 value)
        {
            if (_mapBackward.TryGetValue(value, out var key))
            {
                return key;
            }

            return default;
        }
    }
}
