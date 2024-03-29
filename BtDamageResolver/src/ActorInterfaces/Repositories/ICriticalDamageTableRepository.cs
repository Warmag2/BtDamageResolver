﻿using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for a CriticalDamageTable Repository Actor.
/// </summary>
public interface ICriticalDamageTableRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<CriticalDamageTable, string>
{
}