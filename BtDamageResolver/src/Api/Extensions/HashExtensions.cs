namespace Faemiyah.BtDamageResolver.Api.Extensions;

/// <summary>
/// Simple hashing methods which are independent with respect to the platform.
/// </summary>
public static class HashExtensions
{
    private const ulong FnvOffsetBasis64 = 14695981039346656037;
    private const ulong FnvPrime64 = 1099511628211;
    private const uint FnvOffsetBasis32 = 2166136261;
    private const uint FnvPrime32 = 16777619;

    /// <summary>
    /// Hash a string using the FNV-1a algorithm.
    /// </summary>
    /// <param name="input">The string.</param>
    /// <returns>The resulting FNV-1a hash.</returns>
    public static long Fnv1aHash64(this string input)
    {
        var hash = FnvOffsetBasis64;

        foreach (var c in input)
        {
            hash ^= c;
            hash *= FnvPrime64;
        }

        return unchecked((long)hash);
    }

    /// <summary>
    /// Hash a string using the FNV-1a algorithm.
    /// </summary>
    /// <param name="input">The string.</param>
    /// <returns>The resulting FNV-1a hash.</returns>
    public static int Fnv1aHash32(this string input)
    {
        var hash = FnvOffsetBasis32;

        foreach (var c in input)
        {
            hash ^= c;
            hash *= FnvPrime32;
        }

        return unchecked((int)hash);
    }
}
