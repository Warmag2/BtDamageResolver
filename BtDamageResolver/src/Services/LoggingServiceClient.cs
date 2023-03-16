using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Faemiyah.BtDamageResolver.Services.Interfaces.Enums;
using Orleans.Runtime.Services;

namespace Faemiyah.BtDamageResolver.Services;

/// <summary>
/// The logging service client.
/// </summary>
public class LoggingServiceClient : GrainServiceClient<ILoggingService>, ILoggingServiceClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingServiceClient"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public LoggingServiceClient(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    private ILoggingService GrainService => GetGrainService(CurrentGrainReference.GrainId);

    /// <inheritdoc/>
    public Task LogGameAction(DateTime timeStamp, string gameId, GameActionType gameActionType, int actionData) =>
        GrainService.LogGameAction(timeStamp, gameId, gameActionType, actionData);

    /// <inheritdoc/>
    public Task LogPlayerAction(DateTime timeStamp, string userId, PlayerActionType playerActionType, int actionData) =>
        GrainService.LogPlayerAction(timeStamp, userId, playerActionType, actionData);

    /// <inheritdoc/>
    public Task LogUnitAction(DateTime timeStamp, string unitId, UnitActionType unitActionType, int actionData) =>
        GrainService.LogUnitAction(timeStamp, unitId, unitActionType, actionData);
}