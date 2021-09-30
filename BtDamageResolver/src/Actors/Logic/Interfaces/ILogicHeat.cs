using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces
{
    public interface ILogicHeat
    {
        public void ResolveAttackerHeat(DamageReport damageReport, bool hitHappened, UnitEntry firingUnit, Weapon weapon, WeaponMode mode);
    }
}