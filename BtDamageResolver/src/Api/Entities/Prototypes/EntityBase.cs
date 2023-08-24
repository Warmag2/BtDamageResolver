using System;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;

namespace Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

/// <inheritdoc />
[Serializable]
public abstract class EntityBase<TKey> : IEntity<TKey>
    where TKey : IComparable
{
    /// <inheritdoc />
    public abstract TKey GetId();

    /// <inheritdoc />
    public abstract void SetId(TKey id);
}