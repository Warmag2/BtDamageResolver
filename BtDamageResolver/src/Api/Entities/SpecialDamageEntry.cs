using System;
using System.Diagnostics.CodeAnalysis;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A special damage entry.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Never used for object comparison.")]
public sealed class SpecialDamageEntry : IEquatable<SpecialDamageEntry>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpecialDamageEntry"/> class.
    /// </summary>
    public SpecialDamageEntry()
    {
        Data = "0";
        Type = SpecialDamageType.None;
    }

    /// <summary>
    /// The freeform data for this special damage entry.
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// The type of this special damage entry.
    /// </summary>
    public SpecialDamageType Type { get; set; }

    /// <summary>
    /// Null this special damage entry.
    /// </summary>
    public void Clear()
    {
        Type = SpecialDamageType.None;
        Data = "0";
    }

    /// <summary>
    /// Produce a deep copy of this <see cref="SpecialDamageEntry"/>.
    /// </summary>
    /// <returns>A deep copy of this <see cref="SpecialDamageEntry"/>.</returns>
    public SpecialDamageEntry Copy()
    {
        return new SpecialDamageEntry
        {
            Data = Data,
            Type = Type
        };
    }

    /// <inheritdoc />
    public bool Equals(SpecialDamageEntry other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Data == other.Data && Type == other.Type;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Type} ({Data})";
    }
}