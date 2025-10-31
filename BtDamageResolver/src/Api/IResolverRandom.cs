namespace Faemiyah.BtDamageResolver.Api;

/// <summary>
/// Interface for a resolver-specific random number generator.
/// </summary>
public interface IResolverRandom
{
    /// <summary>
    /// Perform a randomization for throwing two 6-sided dice.
    /// </summary>
    /// <returns>A pseudorandom 2d6 value.</returns>
    int D26();

    /// <summary>
    /// Gives the next random integer between 0 and max-1.
    /// </summary>
    /// <param name="max">The maximum value (noninclusive) of the given integer.</param>
    /// <returns>A pseudorandom integer number.</returns>
    int Next(int max);

    /// <summary>
    /// Gives the next random integer between 1 and max.
    /// </summary>
    /// <param name="max">The maximum value (inclusive) of the given integer.</param>
    /// <returns>A pseudorandom integer number.</returns>
    int NextPlusOne(int max);

    /// <summary>
    /// Gives the next random integer value between 1 and max.
    /// </summary>
    /// <param name="max">The maximum value (inclusive) of the given integer.</param>
    /// <returns>A pseudorandom decimal number.</returns>
    int NextPlusOne(decimal max);
}