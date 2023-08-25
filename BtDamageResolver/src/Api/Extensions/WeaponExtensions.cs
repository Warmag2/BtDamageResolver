using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Extensions;

/// <summary>
/// Weapon extensions.
/// </summary>
public static class WeaponExtensions
{
    /// <summary>
    /// Does the given weapon feature list have the given feature.
    /// </summary>
    /// <param name="weaponFeatures">A list of weapon features.</param>
    /// <param name="feature">A weapon feature type.</param>
    /// <param name="weaponFeatureEntry">The matching weapon feature entry, if any.</param>
    /// <returns><b>True</b> if the weapon feature was found, <b>false</b> otherwise.</returns>
    public static bool HasFeature(this List<WeaponFeatureEntry> weaponFeatures, WeaponFeature feature, out WeaponFeatureEntry weaponFeatureEntry)
    {
        weaponFeatureEntry = weaponFeatures.SingleOrDefault(w => w.Type == feature);

        return weaponFeatureEntry != null;
    }
}