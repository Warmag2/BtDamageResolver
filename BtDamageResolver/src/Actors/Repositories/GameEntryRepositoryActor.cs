using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Base;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories
{
    /// <summary>
    /// An Game Entry repository actor, which stores the properties of different units.
    /// </summary>
    public class GameEntryRepositoryActor : ExternalRepositoryActorBase<GameEntry,string>, IGameEntryRepository
    {
        private readonly TimeSpan _maxGameAge = TimeSpan.FromHours(Settings.MaximumGameEntryAgeHours);

        public GameEntryRepositoryActor(ILogger<GameEntryRepositoryActor> logger, CachedEntityRepository<GameEntry,string> repository) : base(logger, repository)
        {
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

        private async Task CleanupOldEntries()
        {
            foreach (var entry in (await base.GetAll()).Where(gameEntry => gameEntry.TimeStamp < DateTime.UtcNow - _maxGameAge))
            {
                await Delete(entry.GetId());
            }
        }
    }
}