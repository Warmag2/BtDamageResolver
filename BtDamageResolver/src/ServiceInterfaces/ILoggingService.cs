using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces
{
    public interface ILoggingService : IGrainService
    {
        Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData);

        Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData);

        Task LogUnitAction(DateTime timeStamp, string unitId, UnitActionType unitActionType, int actionData);
    }
}
