using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces
{
    /// <summary>
    /// Client for <see cref="ILoggingService"/>.
    /// </summary>
    public interface ILoggingServiceClient : IGrainServiceClient<ILoggingService>, ILoggingService
    {
    }
}