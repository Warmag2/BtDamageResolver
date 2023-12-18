using System;
using System.Diagnostics.CodeAnalysis;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities;

/// <summary>
/// A weapon feature entry.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Never used for object comparison.")]
public sealed class WeaponFeatureEntry : IEquatable<WeaponFeatureEntry>
{
    /// <summary>
    /// Supplementary data needed for weapon feature resolution.
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// The type of this weapon feature.
    /// </summary>
    public WeaponFeature Type { get; set; }

    /// <summary>
    /// Produce a deep copy of this <see cref="WeaponFeatureEntry"/>.
    /// </summary>
    /// <returns>A deep copy of this <see cref="WeaponFeatureEntry"/>.</returns>
    public WeaponFeatureEntry Copy()
    {
        return new WeaponFeatureEntry
        {
            Data = Data,
            Type = Type
        };
    }

    /// <inheritdoc />
    public bool Equals(WeaponFeatureEntry other)
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
        var dataString = Data ?? "null";
        return $"{Type} ({dataString})";
    }
}