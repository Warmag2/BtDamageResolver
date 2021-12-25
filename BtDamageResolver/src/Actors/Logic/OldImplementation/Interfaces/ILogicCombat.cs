using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Options;

namespace Faemiyah.BtDamageResolver.Actors.Logic.Interfaces
{
    public interface ILogicCombat
    {
        Task<List<DamageReport>> Fire(GameOptions gameOptions, UnitEntry firingUnit);

        Task<DamageReport> ResolveDamageInstance(DamageInstance damageInstance, GameOptions gameOptions);
    }
}