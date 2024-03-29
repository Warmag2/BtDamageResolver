﻿using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for a PaperDoll Repository Actor.
/// </summary>
public interface IPaperDollRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<PaperDoll, string>
{
}