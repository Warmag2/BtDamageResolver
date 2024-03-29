﻿using Faemiyah.BtDamageResolver.ActorInterfaces.Repositories.Prototypes;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Orleans;

namespace Faemiyah.BtDamageResolver.ActorInterfaces.Repositories;

/// <summary>
/// Interface for a Weapon Repository Actor.
/// </summary>
public interface IWeaponRepository : IGrainWithIntegerKey, IExternalRepositoryActorBase<Weapon, string>
{
}