// 7Zip helper code, original by Peter Bromberg
// http://www.eggheadcafe.com/tutorials/aspnet/064b41e4-60bc-4d35-9136-368603bcc27a/7zip-lzma-inmemory-com.aspx

using System;
using System.IO;
using System.Text;

namespace SevenZip.Compression.LZMA
{
    public static class CompressionHelper
    {
        static int dictionary = 1 << 23;

        // static Int32 posStateBits = 2;
        // static Int32 litContextBits = 3; // for normal files
        // UInt32 litContextBits = 0; // for 32-bit data
        // static Int32 litPosBits = 0;
        // UInt32 litPosBits = 2; // for 32-bit data
        // static Int32 algorithm = 2;
        // static Int32 numFastBytes = 128;

        private const bool eos = false;

        static CoderPropID[] propIDs =
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
        static object[] properties =
        {
            (Int32) (dictionary),
            (Int32) (2),
            (Int32) (3),
            (Int32) (0),
            (Int32) (2),
            (Int32) (128),
            "bt4",
            eos
        };

        public static byte[] Compress(byte[] inputBytes)
        {
            var inStream = new MemoryStream(inputBytes);
            var outStream = new MemoryStream();
            var encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            
            var inputSize = inStream.Length;
            for (var i = 0; i < 8; i++)
            {
                outStream.WriteByte((byte) (inputSize >> (8 * i)));
            }

            encoder.Code(inStream, outStream, -1, -1, null);
            return outStream.ToArray();
        }
        public static byte[] Decompress(byte[] inputBytes)
        {
            var inStream = new MemoryStream(inputBytes);
            var outStream = new MemoryStream();
            var decoder = new Decoder();

            inStream.Seek(0, 0);

            var decoderProperties = new byte[5];
            if (inStream.Read(decoderProperties, 0, 5) != 5)
            {
                throw (new Exception("input .lzma is too short"));
            }

            long outSize = 0;

            for (var i = 0; i < 8; i++)
            {
                var v = inStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long) (byte) v) << (8 * i);
            }

            decoder.SetDecoderProperties(decoderProperties);

            var compressedSize = inStream.Length - inStream.Position;
            decoder.Code(inStream, outStream, compressedSize, outSize, null);

            var b = outStream.ToArray();

            return b;
        }

        public static byte[] CompressString(string input)
        {
            return Compress(Encoding.UTF8.GetBytes(input));
        }

        public static string DecompressString(byte[] input)
        {
            return Encoding.UTF8.GetString(Decompress(input));
        }
    }
}
