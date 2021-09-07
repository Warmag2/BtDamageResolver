using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces
{
    public interface ILogicAmmo
    {
        public void ResolveAttackerAmmo(DamageReport damageReport, bool hitHappened, Weapon weapon, WeaponMode mode);
    }
}