using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class SpecialDamageEntry
    {
        public SpecialDamageEntry()
        {
            Data = "0";
            Type = SpecialDamageType.None;
        }

        public string Data { get; set; }

        public SpecialDamageType Type { get; set; }

        public override string ToString()
        {
            return $"{Type} ({Data})";
        }

        public void Clear()
        {
            Type = SpecialDamageType.None;
            Data = "0";
        }
    }
}