// 7Zip helper code, original by Peter Bromberg
// http://www.eggheadcafe.com/tutorials/aspnet/064b41e4-60bc-4d35-9136-368603bcc27a/7zip-lzma-inmemory-com.aspx
using System.IO;

namespace SevenZip.Compression.LZMA;

/// <summary>
/// Static support class for help on compressing byte data.
/// </summary>
public static class CompressionHelper
{
    private const bool Eos = false;
    private static readonly int Dictionary = 1 << 23;

    // static Int32 posStateBits = 2;
    // static Int32 litContextBits = 3; // for normal files
    // UInt32 litContextBits = 0; // for 32-bit data
    // static Int32 litPosBits = 0;
    // UInt32 litPosBits = 2; // for 32-bit data
    // static Int32 algorithm = 2;
    // static Int32 numFastBytes = 128;
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

    // these are the default properties, keeping it simple for now:
    private static readonly object[] Properties =
    {
        Dictionary,
        2,
        3,
        0,
        2,
        128,
        "bt4",
        Eos
    };

    /// <summary>
    /// Compress data.
    /// </summary>
    /// <param name="inputBytes">Input data.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Compress(byte[] inputBytes)
    {
        var inStream = new MemoryStream(inputBytes);
        var outStream = new MemoryStream();
        var encoder = new Encoder();
        encoder.SetCoderProperties(PropIDs, Properties);
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
}
