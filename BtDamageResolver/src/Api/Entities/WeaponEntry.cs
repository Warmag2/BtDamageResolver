using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class WeaponEntry
    {
        public WeaponEntry()
        {
            Id = Guid.NewGuid();
        }

        public DateTime TimeStamp { get; set; }

        public Guid Id { get; set; }

        public string WeaponName { get; set; }

        public WeaponState State { get; set; }

        public WeaponMode Mode { get; set; }

        public WeaponEntry Copy()
        {
            return new WeaponEntry
            {
                TimeStamp = DateTime.UtcNow,
                Mode = Mode,
                State = State,
                WeaponName = WeaponName
            };
        }
    }
}