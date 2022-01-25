using Faemiyah.BtDamageResolver.Api.Enums;
using System;

namespace Faemiyah.BtDamageResolver.Actors.Logic
{
    /// <summary>
    /// Base implementations of public methods for unit logic, overridden by actual unit type implementations.
    /// </summary>
    public partial class LogicUnit
    {
        /// <inheritdoc />
        public virtual bool CanTakeEmpHits()
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool CanTakeMotiveHits()
        {
            return false;
        }

        /// <inheritdoc />
        public Guid GetId() => Unit.Id;

        /// <inheritdoc />
        public string GetName() => Unit.Name;

        /// <inheritdoc />
        public abstract PaperDollType GetPaperDollType();

        /// <inheritdoc />
        public Stance GetStance() => Unit.Stance;

        /// <inheritdoc />
        public int GetTonnage() => Unit.Tonnage;

        /// <inheritdoc />
        public int GetTroopers() => Unit.Troopers;

        /// <inheritdoc />
        public UnitType GetUnitType() => Unit.Type;

        /// <inheritdoc />
        public bool HasFeature(UnitFeature unitFeature) => Unit.HasFeature(unitFeature);


        /// <inheritdoc />
        public virtual bool IsBlockedByCover(Cover cover, Location location)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual bool IsHeatTracking()
        {
            return false;
        }

        /// <inheritdoc />
        public bool IsGlancingBlow(int marginOfSuccess)
        {
            return Unit.HasFeature(UnitFeature.NarrowLowProfile) && marginOfSuccess == 0;
        }

        /// <inheritdoc />
        public bool IsNarced() => Unit.Narced;

        /// <inheritdoc />
        public bool IsTagged() => Unit.Tagged;
    }
}
