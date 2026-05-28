using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SevenZip.Compression.LZMA;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;

/// <summary>
/// Static easy-to-use compression helping methods for compressing complex types through JSON serialization.
/// </summary>
public class DataHelper
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly CompressionOptions _compressionOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHelper"/> class.
    /// </summary>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="compressionOptions">The compression options.</param>
    public DataHelper(IOptions<JsonSerializerOptions> jsonSerializerOptions, IOptions<CompressionOptions> compressionOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions.Value;
        _compressionOptions = compressionOptions.Value;
    }

    /// <summary>
    /// Serialize an entity and compress the byte array.
    /// </summary>
    /// <typeparam name="TType">The type to compress.</typeparam>
    /// <param name="input">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public byte[] Pack<TType>(TType input)
        where TType : class
    {
        return Compress(Serialize(input));
    }

    /// <summary>
    /// Decompress a byte array and deserialize entity.
    /// </summary>
    /// <param name="input">The byte array to decompress.</param>
    /// <typeparam name="TType">The compressed type.</typeparam>
    /// <returns>The decompressed byte array.</returns>
    public TType Unpack<TType>(byte[] input)
        where TType : class
    {
        return Deserialize<TType>(Decompress(input));
    }

    /// <summary>
    /// Deserialize a byte array into an object.
    /// </summary>
    /// <typeparam name="TType">The type of the object.</typeparam>
    /// <param name="input">The byte array.</param>
    /// <returns>The deserialized object.</returns>
    private TType Deserialize<TType>(byte[] input)
        where TType : class
    {
        return JsonSerializer.Deserialize<TType>(input, _jsonSerializerOptions);
    }

    /// <summary>
    /// Serialize an object to a byte array.
    /// </summary>
    /// <typeparam name="TType">The type of the object.</typeparam>
    /// <param name="input">The object.</param>
    /// <returns>The serialized byte array.</returns>
    private byte[] Serialize<TType>(TType input)
        where TType : class
    {
        return JsonSerializer.SerializeToUtf8Bytes(input, _jsonSerializerOptions);
    }

    private byte[] Compress(byte[] input)
    {
        return _compressionOptions.Provider switch
        {
            CompressionProvider.Brotli => BrotliCompress(input, _compressionOptions.Quality),
            CompressionProvider.Lzma => CompressionHelper.Compress(input, _compressionOptions.Quality),
            _ => throw new InvalidOperationException($"Unsupported compression provider: {_compressionOptions.Provider}")
        };
    }

    private byte[] Decompress(byte[] input)
    {
        return _compressionOptions.Provider switch
        {
            CompressionProvider.Brotli => BrotliDecompress(input),
            CompressionProvider.Lzma => CompressionHelper.Decompress(input),
            _ => throw new InvalidOperationException($"Unsupported compression provider: {_compressionOptions.Provider}")
        };
    }

    private static byte[] BrotliCompress(byte[] input, int quality)
    {
        var clamped = Math.Clamp(quality, 0, 11);
        var maxLength = BrotliEncoder.GetMaxCompressedLength(input.Length);
        var output = new byte[maxLength];

        if (!BrotliEncoder.TryCompress(input, output, out var bytesWritten, clamped, window: 22))
        {
            throw new InvalidOperationException("Brotli compression failed: destination buffer too small.");
        }

        return output.AsSpan(0, bytesWritten).ToArray();
    }

    private static byte[] BrotliDecompress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var outputStream = new MemoryStream();
        using var brotli = new BrotliStream(inputStream, CompressionMode.Decompress);
        brotli.CopyTo(outputStream);

        return outputStream.ToArray();
    }
}
