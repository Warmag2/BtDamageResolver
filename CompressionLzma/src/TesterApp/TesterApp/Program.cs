// This example by Chris Hulbert http://splinter.com.au/blog
// 7Zip helper code by Peter Bromberg
// http://www.eggheadcafe.com/tutorials/aspnet/064b41e4-60bc-4d35-9136-368603bcc27a/7zip-lzma-inmemory-com.aspx
// 7Zip LZMA compression by Igor Pavlov http://www.7-zip.org/sdk.html

using System;
using System.Text;

namespace SevenZip.Compression.LZMA
{
    class Program
    {
        static void Main(string[] args)
        {
            // Some random text to compress, thanks to http://en.wikipedia.org/wiki/V8
            var OriginalText =
      @"From A V8 engine is a V engine with eight cylinders mounted on the crankcase
in two banks of four cylinders, in most cases set at a right angle to each other
but sometimes at a narrower angle, with all eight pistons driving a common crankshaft.
In its simplest form, it is basically two straight-4 engines sharing a common
crankshaft. However, this simple configuration, with a single-plane crankshaft,
has the same secondary dynamic imbalance problems as two straight-4s, resulting
in annoying vibrations in large engine displacements. As a result, since the 1920s
most V8s have used the somewhat more complex crossplane crankshaft with heavy
counterweights to eliminate the vibrations. This results in an engine which is
smoother than a V6, while being considerably less expensive than a V12 engine.
Racing V8s continue to use the single plane crankshaft because it allows faster
acceleration and more efficient exhaust system designs.";

            // Convert the text into bytes
            var dataBytes = Encoding.ASCII.GetBytes(OriginalText);
            Console.WriteLine("Original data is {0} bytes", dataBytes.Length);

            // Compress it
            var compressed = CompressionHelper.Compress(dataBytes);
            Console.WriteLine("Compressed data is {0} bytes", compressed.Length);

            // Decompress it
            var decompressed = CompressionHelper.Decompress(compressed);
            Console.WriteLine("Decompressed data is {0} bytes", decompressed.Length);

            // Convert it back to text
            var decompressedText = Encoding.ASCII.GetString(decompressed);
            Console.WriteLine("Is the decompressed text the same as the original? {0}", decompressedText == OriginalText);

            // Print it out
            Console.WriteLine("And here is the decompressed text:");
            Console.WriteLine(decompressedText);

            var anotherOriginalText = "Kusipää suutu jo!";
            var byteVersion = CompressionHelper.CompressString(anotherOriginalText);
            var stringVersion = CompressionHelper.DecompressString(byteVersion);
            Console.WriteLine(anotherOriginalText);
            Console.WriteLine(stringVersion);
            Console.WriteLine(anotherOriginalText.Equals(stringVersion));
        }
    }
}
