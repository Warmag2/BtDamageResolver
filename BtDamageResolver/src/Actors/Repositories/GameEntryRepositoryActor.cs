using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Events;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// An Game Entry repository actor, which stores the properties of different units.
    /// </summary>
    public class GameEntryRepositoryActor : ExternalRepositoryActorBase<GameEntry,string>, IGameEntryRepository
    {
        private readonly ICommunicationServiceClient _communicationServiceClient;
        private readonly TimeSpan _maxGameAge = TimeSpan.FromHours(Settings.MaximumGameEntryAgeHours);

        public GameEntryRepositoryActor(ILogger<GameEntryRepositoryActor> logger, CachedEntityRepository<GameEntry,string> repository, ICommunicationServiceClient communicationServiceClient) : base(logger, repository)
        {
            _communicationServiceClient = communicationServiceClient;
        }

        /// <inheritdoc/>
        public override async Task<GameEntry> Get(string key)
        {
            await CleanupOldEntries();
            
            return await base.Get(key);
        }

        /// <inheritdoc/>
        public override async Task<List<GameEntry>> GetAll()
        {
            await CleanupOldEntries();
            
            return await base.GetAll();
        }

        /// <inheritdoc/>
        public override async Task Add(GameEntry entity)
        {
            await base.Add(entity);
            await Distribute();
        }

        /// <inheritdoc/>
        public override async Task AddOrUpdate(GameEntry entity)
        {
            await base.AddOrUpdate(entity);
            await Distribute();
        }

        /// <inheritdoc/>
        public override async Task<bool> Delete(string key)
        {
            var result = await base.Delete(key);
            await Distribute();

            return result;
        }

        /// <inheritdoc/>
        public override async Task Update(GameEntry entity)
        {
            await base.Update(entity);
            await Distribute();
        }

        private async Task Distribute()
        {
            await _communicationServiceClient.SendToAllClients(EventNames.GameEntries, await GetAll());
        }

        private async Task CleanupOldEntries()
        {
            foreach (var entry in (await base.GetAll()).Where(gameEntry => gameEntry.TimeStamp < DateTime.UtcNow - _maxGameAge))
            {
                await Delete(entry.GetId());
            }
        }
    }
}