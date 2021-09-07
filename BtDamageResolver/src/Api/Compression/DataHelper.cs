using System.Text;
using Newtonsoft.Json;

namespace Faemiyah.BtDamageResolver.Api.Compression
{
    public static class DataHelper
    {
        /// <summary>
        /// Serialize an object to a byte array.
        /// </summary>
        /// <typeparam name="TType">The type of the object.</typeparam>
        /// <param name="input">The object.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize<TType>(TType input) where TType : class
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
        }

        /// <summary>
        /// Deserialize a byte array into an object.
        /// </summary>
        /// <typeparam name="TType">The type of the object.</typeparam>
        /// <param name="input">The byte array.</param>
        /// <returns>The deserialized object.</returns>
        public static TType Deserialize<TType>(byte[] input) where TType : class
        {
            return JsonConvert.DeserializeObject<TType>(Encoding.UTF8.GetString(input));
        }

        /// <summary>
        /// Compress a byte array using LZMA compression.
        /// </summary>
        /// <param name="input">The byte array to compress.</param>
        /// <returns>The compressed byte array.</returns>
        public static byte[] Compress(byte[] input)
        {
            return SevenZip.Compression.LZMA.CompressionHelper.Compress(input);
        }

        /// <summary>
        /// Decompress a byte array using LZMA compression.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <returns>The decompressed byte array.</returns>
        public static byte[] Decompress(byte[] input)
        {
            return SevenZip.Compression.LZMA.CompressionHelper.Decompress(input);
        }

        /// <summary>
        /// Serialize an entity and compress the byte array.
        /// </summary>
        /// <param name="input">The byte array to compress.</param>
        /// <returns>The compressed byte array.</returns>
        public static byte[] Pack<TType>(TType input) where TType : class
        {
            return Compress(Serialize(input));
        }

        /// <summary>
        /// Decompress a byte array and deserialize entity.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <returns>The decompressed byte array.</returns>
        public static TType Unpack<TType>(byte[] input) where TType : class
        {
            return Deserialize<TType>(Decompress(input));
        }
    }
}