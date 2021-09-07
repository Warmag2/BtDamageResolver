using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class CriticalDamageTable : EntityBase<string>
    {
        public Location Location { get; set; }

        public Dictionary<int, List<CriticalDamageType>> Mapping { get; set; }

        public CriticalDamageTableType Type { get; set; }

        public PaperDollType UnitType { get; set; }

        /// <summary>
        /// Needed because the id is concatenated from several properties in this critical damage table.
        /// </summary>
        /// <returns></returns>
        public override string GetId()
        {
            return GetIdFromProperties(UnitType, Type, Location);
        }

        public static string GetIdFromProperties(PaperDollType unitType, CriticalDamageTableType criticalDamageTableType, Location location)
        {
            return $"{unitType}_{criticalDamageTableType}_{location}";
        }

        public override void SetId(string id)
        {
            throw new InvalidOperationException("You should never have to set a PaperDoll Id manually.");
        }

        public override EntityValidationResult Validate()
        {
            var entityValidationResult = new EntityValidationResult();

            EntitySpecificValidate(entityValidationResult);

            return entityValidationResult;
        }

        protected override void EntitySpecificValidate(EntityValidationResult validationResult)
        {
            if (Mapping.Keys.Count != 11)
            {
                validationResult.Disqualify($"Not correct amount of locations for a critical damage map. Expected 11, found {Mapping.Keys.Count}");
            }
        }
    }
}
