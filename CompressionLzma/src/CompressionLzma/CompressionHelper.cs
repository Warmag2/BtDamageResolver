// 7Zip helper code, original by Peter Bromberg
// http://www.eggheadcafe.com/tutorials/aspnet/064b41e4-60bc-4d35-9136-368603bcc27a/7zip-lzma-inmemory-com.aspx
using System;
using System.IO;

namespace SevenZip.Compression.LZMA;

/// <summary>
/// Static support class for help on compressing byte data.
/// </summary>
public static class CompressionHelper
{
    private const bool Eos = false;
    private const int MinNumFastBytes = 5;
    private const int MaxNumFastBytes = 273;
    private static readonly int Dictionary = 1 << 23;

    private static readonly CoderPropID[] PropIDs =
    {
        CoderPropID.DictionarySize,
        CoderPropID.PosStateBits,
        CoderPropID.LitContextBits,
        CoderPropID.LitPosBits,
        CoderPropID.Algorithm,
        CoderPropID.NumFastBytes,
        CoderPropID.MatchFinder,
        CoderPropID.EndMarker
    };

    /// <summary>
    /// Compress data.
    /// </summary>
    /// <param name="inputBytes">Input data.</param>
    /// <param name="quality">Compression quality 0-11. Higher = better ratio but more CPU. Maps internally to LZMA's NumFastBytes (5-273).</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Compress(byte[] inputBytes, int quality = 4)
    {
        var inStream = new MemoryStream(inputBytes);
        var outStream = new MemoryStream();
        var encoder = new Encoder();
        encoder.SetCoderProperties(PropIDs, BuildProperties(quality));
        encoder.WriteCoderProperties(outStream);

        var inputSize = inStream.Length;
        for (var i = 0; i < 8; i++)
        {
            outStream.WriteByte((byte)(inputSize >> (8 * i)));
        }

        encoder.Code(inStream, outStream, -1, -1, null);
        return outStream.ToArray();
    }

    /// <summary>
    /// Decompress data.
    /// </summary>
    /// <param name="inputBytes">Input byte array.</param>
    /// <returns>Uncompressed byte array.</returns>
    public static byte[] Decompress(byte[] inputBytes)
    {
        var inStream = new MemoryStream(inputBytes);
        var outStream = new MemoryStream();
        var decoder = new Decoder();

        inStream.Seek(0, 0);

        var decoderProperties = new byte[5];
        if (inStream.Read(decoderProperties, 0, 5) != 5)
        {
            throw new InvalidDataException("input .lzma is too short");
        }

        long outSize = 0;

        for (var i = 0; i < 8; i++)
        {
            var v = inStream.ReadByte();
            if (v < 0)
            {
                throw new EndOfStreamException("Can't Read 1");
            }

            outSize |= ((long)(byte)v) << (8 * i);
        }

        decoder.SetDecoderProperties(decoderProperties);

        var compressedSize = inStream.Length - inStream.Position;
        decoder.Code(inStream, outStream, compressedSize, outSize, null);

        var b = outStream.ToArray();

        return b;
    }

    private static object[] BuildProperties(int quality)
    {
        var clamped = Math.Clamp(quality, MinNumFastBytes, MaxNumFastBytes);

        return
        [
            Dictionary,
            2,                  // PosStateBits
            3,                  // LitContextBits
            0,                  // LitPosBits
            2,                  // Algorithm (binary tree)
            clamped,
            "bt4",
            Eos,
        ];
    }
}
