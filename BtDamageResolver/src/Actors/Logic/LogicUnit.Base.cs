using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Actors.Logic;

/// <summary>
/// Base implementations of public methods for unit logic, overridden by actual unit type implementations.
/// </summary>
public partial class LogicUnit
{
    /// <inheritdoc />
    public virtual bool CanTakeCriticalHits()
    {
        return true;
    }

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
    public abstract PaperDollType GetPaperDollType();

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
}