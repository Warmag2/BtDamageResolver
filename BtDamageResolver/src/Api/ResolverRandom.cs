using System;

namespace Faemiyah.BtDamageResolver.Api;

/// <summary>
/// A resolver-specific random number generator.
/// </summary>
/// <remarks>
/// Backed by <see cref="Random.Shared"/>, which is thread-safe. This type is registered as a DI singleton and used
/// concurrently by all <c>LogicUnit</c> instances across all grain activations, so the underlying RNG must be
/// thread-safe to avoid the well-known <see cref="Random"/> race condition where concurrent calls can produce
/// repeated zeros or trigger infinite loops in callers.
/// </remarks>
public class ResolverRandom : IResolverRandom
{
    /// <inheritdoc/>
    public int D26()
    {
        return DX(6) + DX(6);
    }

    /// <inheritdoc/>
    public int Next(int max)
    {
        return Random.Shared.Next(max);
    }

    /// <inheritdoc/>
    public int DX(int max)
    {
        return Random.Shared.Next(max) + 1;
    }

    /// <inheritdoc/>
    public int DX(decimal max)
    {
        return DX(decimal.ToInt32(max));
    }
}