using Orleans;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces;

/// <summary>
/// Client for <see cref="ICommunicationService"/>.
/// </summary>
[Alias("ICommunicationServiceClient")]
public interface ICommunicationServiceClient : IGrainServiceClient<ICommunicationService>, ICommunicationService
{
}