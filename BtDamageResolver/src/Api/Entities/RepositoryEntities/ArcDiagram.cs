using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;

/// <summary>
/// Diagram listing the firing arcs and possible bay locations of an unit.
/// </summary>
[Serializable]
public class ArcDiagram : EntityBase<string>
{
    /// <summary>
    /// The collection of firing arcs for an unit type.
    /// </summary>
    public HashSet<Arc> Arcs { get; set; }

    /// <summary>
    /// The unit type for these arcs.
    /// </summary>
    public UnitType UnitType { get; set; }

    /// <inheritdoc />
    public override string GetId() => UnitType.ToString();

    /// <inheritdoc />
    public override void SetId(string id)
    {
        throw new InvalidOperationException("You should never have to set a PaperDoll Id manually.");
    }
}