using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces;

/// <summary>
/// Client for <see cref="ICommunicationService"/>.
/// </summary>
public interface ICommunicationServiceClient : IGrainServiceClient<ICommunicationService>, ICommunicationService
{
}