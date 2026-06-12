using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;
using Faemiyah.BtDamageResolver.Actors.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.Constants;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faemiyah.BtDamageResolver.Actors.Repositories;

/// <summary>
/// An Game Entry repository actor, which stores information on ongoing games.
/// </summary>
public class GameEntryRepositoryActor : ExternalRepositoryActorBase<GameEntry, string>, IGameEntryRepository
{
    private static readonly TimeSpan ExpireInterval = TimeSpan.FromMinutes(5);

    private readonly ICommunicationServiceClient _communicationServiceClient;
    private readonly TimeSpan _maxGameAge = TimeSpan.FromHours(Settings.MaximumGameEntryAgeHours);
    private DateTime _lastExpire = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEntryRepositoryActor"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    /// <param name="repository">The repository back-end.</param>
    /// <param name="communicationServiceClient">The communication service client.</param>
    public GameEntryRepositoryActor(ILogger<GameEntryRepositoryActor> logger, CachedEntityRepository<GameEntry, string> repository, ICommunicationServiceClient communicationServiceClient) : base(logger, repository)
    {
        _communicationServiceClient = communicationServiceClient;
    }

    /// <inheritdoc/>
    public override async Task<GameEntry> Get(string key)
    {
        return await base.Get(key);
    }

    /// <inheritdoc/>
    public override async Task<IReadOnlyCollection<GameEntry>> GetAll()
    {
        await ExpireOldEntries();

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

    private async Task ExpireOldEntries()
    {
        // Throttle expiration scans — entries age in hours so checking on every Get/GetAll
        // (which the broadcast path also hits) is wasteful. When old entries are actually
        // deleted, broadcast once at the end so connected lobby clients see the new list.
        var now = DateTime.UtcNow;
        if (now - _lastExpire < ExpireInterval)
        {
            return;
        }

        _lastExpire = now;

        foreach (var entry in (await base.GetAll()).Where(gameEntry => gameEntry.TimeStamp < now - _maxGameAge))
        {
            await base.Delete(entry.GetName());
        }
    }
}