using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Orleans.Runtime.Services;

namespace Faemiyah.BtDamageResolver.Services
{
    public class CommunicationServiceClient : GrainServiceClient<ICommunicationService>, ICommunicationServiceClient
    {
        public CommunicationServiceClient(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Task Send(string playerId, string envelopeType, object data) => GrainService.Send(playerId, envelopeType, data);
    }
}