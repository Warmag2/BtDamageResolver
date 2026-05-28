using System;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;

/// <summary>
/// Defines options for on-wire compression of Redis pub/sub payloads.
/// </summary>
[Serializable]
public class CompressionOptions
{
    /// <summary>
    /// The compression algorithm to use. Server and client must agree.
    /// </summary>
    public CompressionProvider Provider { get; set; } = CompressionProvider.Lzma;

    /// <summary>
    /// The compression quality (0-11). Higher = better ratio but more CPU; 4 is a balanced default.
    /// Mapped through to <c>BrotliEncoder</c> quality for Brotli and <c>NumFastBytes</c> for LZMA.
    /// </summary>
    /// <remarks>
    /// Brotli accepts 0-11, where 0 is no compression and 11 is maximum compression.
    /// LZMA accepts 5-275, where 5 is some compression 273 is more compression.
    /// Values above or below the ranges for the provider will be clamped to the maximum.
    /// </remarks>
    public int Quality { get; set; } = 4;
}
