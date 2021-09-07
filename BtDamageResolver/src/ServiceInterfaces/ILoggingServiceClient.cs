using Orleans.Services;

namespace Faemiyah.BtDamageResolver.Services.Interfaces
{
    public interface ILoggingServiceClient : IGrainServiceClient<ILoggingService>, ILoggingService
    {
    }
}