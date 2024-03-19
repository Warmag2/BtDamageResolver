using Orleans;
using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces;

/// <summary>
/// Client for <see cref="ILoggingService"/>.
/// </summary>
[Alias("ILoggingServiceClient")]
public interface ILoggingServiceClient : IGrainServiceClient<ILoggingService>, ILoggingService
{
}