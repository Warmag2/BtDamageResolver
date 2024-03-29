﻿using System.Text;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHelper"/> class.
    /// </summary>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    public DataHelper(IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions.Value;
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
        return CompressionHelper.Compress(Serialize(input));
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
        return Deserialize<TType>(CompressionHelper.Decompress(input));
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
        return JsonSerializer.Deserialize<TType>(Encoding.UTF8.GetString(input), _jsonSerializerOptions);
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
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input, _jsonSerializerOptions));
    }
}