using System.Text;
using System.Text.Json;

namespace SevenZip.Compression.LZMA
{
    /// <summary>
    /// Static easy-to-use compression helping methods for compressing complex types through JSON serialization.
    /// </summary>
    public class DataHelper
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public DataHelper(JsonSerializerOptions jsonSerializerOptions)
        {
            _jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <summary>
        /// Serialize an object to a byte array.
        /// </summary>
        /// <typeparam name="TType">The type of the object.</typeparam>
        /// <param name="input">The object.</param>
        /// <returns>The serialized byte array.</returns>
        public string Serialize<TType>(TType input) where TType : class
        {
            return JsonSerializer.Serialize(input, _jsonSerializerOptions);
        }

        /// <summary>
        /// Deserialize a byte array into an object.
        /// </summary>
        /// <typeparam name="TType">The type of the object.</typeparam>
        /// <param name="input">The byte array.</param>
        /// <returns>The deserialized object.</returns>
        public TType Deserialize<TType>(string input) where TType : class
        {
            return JsonSerializer.Deserialize<TType>(input, _jsonSerializerOptions);
        }

        /// <summary>
        /// Serialize an entity and compress the byte array.
        /// </summary>
        /// <param name="input">The byte array to compress.</param>
        /// <returns>The compressed byte array.</returns>
        public byte[] Pack<TType>(TType input) where TType : class
        {
            return CompressionHelper.Compress(Encoding.UTF8.GetBytes(Serialize(input)));
        }

        /// <summary>
        /// Decompress a byte array and deserialize entity.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <returns>The decompressed byte array.</returns>
        public TType Unpack<TType>(byte[] input) where TType : class
        {
            return Deserialize<TType>(Encoding.UTF8.GetString(CompressionHelper.Decompress(input)));
        }
    }
}