using System;
using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class ValidationExtensions
    {
        public static bool AllEnumValuesArePresentExactlyOnce<TKeyType, TValueType>(this IDictionary<TKeyType, TValueType> input) where TKeyType : System.Enum
        {
            if (input == null)
            {
                return false;
            }

            var enumValues = (Enum.GetValues(typeof(TKeyType)) as IEnumerable<TKeyType>).ToList();

            if (enumValues.Count != input.Keys.Count)
            {
                return false;
            }

            foreach (var enumValue in enumValues)
            {
                if (!input.ContainsKey(enumValue))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AndAllSpecifiedEnumValuesArePresent<TKeyType, TValueType>(this IDictionary<TKeyType, TValueType> input, List<TKeyType> enums) where TKeyType : System.Enum
        {
            if (input == null)
            {
                return false;
            }

            foreach (var enumValue in enums)
            {
                if (!input.ContainsKey(enumValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}