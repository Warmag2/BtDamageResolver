using System;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// A weapon entry portraying a weapon and its current settings.
    /// </summary>
    [Serializable]
    public class WeaponEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponEntry"/> class.
        /// </summary>
        /// <remarks>
        /// Randomizes ID when created.
        /// </remarks>
        public WeaponEntry()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// The timestamp when this weapon entry was last updated.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The ID of this weapon entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the weapon this weapon entry refers to.
        /// </summary>
        public string WeaponName { get; set; }

        /// <summary>
        /// The state of the weapon.
        /// </summary>
        public WeaponState State { get; set; }

        /// <summary>
        /// The name of the ammo to use.
        /// </summary>
        public string Ammo { get; set; }

        /// <summary>
        /// Makes a copy of this weapon entry.
        /// </summary>
        /// <returns>A copy of the weapon entry.</returns>
        public WeaponEntry Copy()
        {
            return new WeaponEntry
            {
                Ammo = Ammo,
                TimeStamp = DateTime.UtcNow,
                State = State,
                WeaponName = WeaponName
            };
        }
    }
}