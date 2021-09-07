using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Orleans.Runtime.Services;

namespace Faemiyah.BtDamageResolver.Services
{
    public class LoggingServiceClient : GrainServiceClient<ILoggingService>, ILoggingServiceClient
    {
        public LoggingServiceClient(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData) =>
            GrainService.LogGameAction(timeStamp, gameId, gameActionType, actionData);

        public Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData) =>
            GrainService.LogPlayerAction(timeStamp, userId, playerActionType, actionData);

        public Task LogUnitAction(DateTime timeStamp, string unitId, UnitActionType unitActionType, int actionData) =>
            GrainService.LogUnitAction(timeStamp, unitId, unitActionType, actionData);
    }
}