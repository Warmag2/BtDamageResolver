using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class FiringSolution
    {
        public int AttackModifier { get; set; }
        
        public Cover Cover { get; set; }

        public Direction Direction { get; set; }

        public int Distance { get; set; }
        
        public Guid TargetUnit { get; set; }

        public FiringSolution Copy()
        {
            return new FiringSolution
            {
                AttackModifier = AttackModifier,
                Cover = Cover,
                Direction = Direction,
                Distance = Distance,
                TargetUnit = TargetUnit
            };
        }
    }
}
