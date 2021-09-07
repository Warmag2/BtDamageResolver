using System;
using System.Threading.Tasks;
using Orleans;

namespace Faemiyah.BtDamageResolver.ClientInterfaces
{
    /// <summary>
    /// The main client-side interface of the resolver.
    /// </summary>
    public interface IResolverClient : IGrainObserver
    {
        Task SendCompressedData(byte[] data, Type dataType);

        Task SendErrorMessage(string errorMessage);
    }
}