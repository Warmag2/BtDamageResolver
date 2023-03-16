using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Orleans.Runtime.Services;

namespace Faemiyah.BtDamageResolver.Services;

/// <summary>
/// Client for the BtDamageResolver server-to-client communication service.
/// </summary>
public class CommunicationServiceClient : GrainServiceClient<ICommunicationService>, ICommunicationServiceClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationServiceClient"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public CommunicationServiceClient(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    private ICommunicationService GrainService => GetGrainService(CurrentGrainReference.GrainId);

    /// <inheritdoc />
    public Task Send(string playerId, string envelopeType, object data) => GrainService.Send(playerId, envelopeType, data);

    /// <inheritdoc />
    public Task SendToMany(List<string> playerIds, string envelopeType, object data) => GrainService.SendToMany(playerIds, envelopeType, data);

    /// <inheritdoc />
    public Task SendToAllClients(string envelopeType, object data) => GrainService.SendToAllClients(envelopeType, data);
}