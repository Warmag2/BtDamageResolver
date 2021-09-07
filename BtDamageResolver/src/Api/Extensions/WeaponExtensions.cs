using System.Collections.Generic;
using System.Linq;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Extensions
{
    public static class WeaponExtensions
    {
        public static bool HasFeature(this List<WeaponFeatureEntry> weaponFeatures, WeaponFeature feature, out WeaponFeatureEntry weaponFeatureEntry)
        {
            weaponFeatureEntry = weaponFeatures.SingleOrDefault(w => w.Type == feature);

            return weaponFeatureEntry != null;
        }
    }
}