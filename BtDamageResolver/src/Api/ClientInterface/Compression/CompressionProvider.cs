namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;

/// <summary>
/// Compression algorithms supported for on-wire Redis pub/sub payloads.
/// </summary>
public enum CompressionProvider
{
    /// <summary>
    /// LZMA via the project-local <c>CompressionLzma</c> implementation. Highest ratio, slowest CPU.
    /// </summary>
    Lzma,

    /// <summary>
    /// Brotli via <c>System.IO.Compression</c>. Much faster than LZMA at comparable ratios for small JSON.
    /// </summary>
    Brotli,
}
