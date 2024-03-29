﻿using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for a ClusterTable Repository Actor.
/// </summary>
public interface IClusterTableRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<ClusterTable, string>
{
}