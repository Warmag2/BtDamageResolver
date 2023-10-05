using System.Text;
using Newtonsoft.Json;
using SevenZip.Compression.LZMA;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;

/// <summary>
/// Static easy-to-use compression helping methods for compressing complex types through JSON serialization.
/// </summary>
public static class DataHelper
{
    /// <summary>
    /// Serialize an entity and compress the byte array.
    /// </summary>
    /// <typeparam name="TType">The type to compress.</typeparam>
    /// <param name="input">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Pack<TType>(TType input)
        where TType : class
    {
        return CompressionHelper.Compress(Serialize(input));
    }

    /// <summary>
    /// Decompress a byte array and deserialize entity.
    /// </summary>
    /// <typeparam name="TType">The type to uncompress.</typeparam>
    /// <param name="input">The byte array to decompress.</param>
    /// <returns>The decompressed byte array.</returns>
    public static TType Unpack<TType>(byte[] input)
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
    private static TType Deserialize<TType>(byte[] input)
        where TType : class
    {
        return JsonConvert.DeserializeObject<TType>(Encoding.UTF8.GetString(input));
    }

    /// <summary>
    /// Serialize an object to a byte array.
    /// </summary>
    /// <typeparam name="TType">The type of the object.</typeparam>
    /// <param name="input">The object.</param>
    /// <returns>The serialized byte array.</returns>
    private static byte[] Serialize<TType>(TType input)
        where TType : class
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
    }
}