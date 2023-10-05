// This example by Chris Hulbert http://splinter.com.au/blog
// 7Zip helper code by Peter Bromberg
// http://www.eggheadcafe.com/tutorials/aspnet/064b41e4-60bc-4d35-9136-368603bcc27a/7zip-lzma-inmemory-com.aspx
// 7Zip LZMA compression by Igor Pavlov http://www.7-zip.org/sdk.html
using System;
using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Compression;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Main program class.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main program entrypoint.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        // Some random text to compress, thanks to http://en.wikipedia.org/wiki/V8
        var originalText =
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

        // Compress it
        var compressed = DataHelper.Pack(originalText);
        Console.WriteLine("Compressed data is {0} bytes", compressed.Length);

        // Decompress it
        var decompressed = DataHelper.Unpack<string>(compressed);
        Console.WriteLine("Decompressed data is {0} bytes", decompressed.Length);

        Console.WriteLine("Is the decompressed text the same as the original? {0}", decompressed == originalText);

        // Print it out
        Console.WriteLine("And here is the decompressed text:");
        Console.WriteLine(decompressed);

        var originalComplexType = new ComplexType
        {
            Uuid = Guid.NewGuid(),
            Dict = new Dictionary<string, int>
            {
                { "nakki", 715517 },
                { "vahvero", 666 }
            }
        };

        var originalComplexTypeCompressed = DataHelper.Pack(originalComplexType);
        var originalComplexTypeUncompressed = DataHelper.Unpack<ComplexType>(originalComplexTypeCompressed);
        var originalAsText = System.Text.Json.JsonSerializer.Serialize(originalComplexType);
        var uncompressedAsText = System.Text.Json.JsonSerializer.Serialize(originalComplexTypeUncompressed);
        Console.WriteLine(originalAsText);
        Console.WriteLine(uncompressedAsText);
        Console.WriteLine(originalAsText.Equals(uncompressedAsText));
    }
}
