using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class EnumExtensions
    {
        public static List<TEnum> GetEnumValueList<TEnum>() where TEnum : Enum
        {
            var enumValueList = new List<TEnum>();

            foreach (var enumValue in Enum.GetValues(typeof(TEnum)))
            {
                enumValueList.Add((TEnum)enumValue);
            }

            return enumValueList;
        }
    }
}