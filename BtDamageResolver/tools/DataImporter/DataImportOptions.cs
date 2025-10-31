using CommandLine;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter;

/// <summary>
/// Data importing options.
/// </summary>
[Verb("import")]
internal sealed class DataImportOptions
{
    /// <summary>
    /// The folder to process.
    /// </summary>
    [Option('f', "folder", Required = false, HelpText = "Folder to load data from.", Default = "./data/")]
    public string Folder { get; set; }

    /// <summary>
    /// Import or not. If set, will produce a dry-run.
    /// </summary>
    [Option('d', "dry-run", Required = false, HelpText = "If set, will only print the deserialized data and not import.", Default = false)]
    public bool DryRun { get; set; }

    /// <summary>
    /// Import only a subset of entries which have this text in their filename.
    /// </summary>
    [Option('s', "substring", Required = false, HelpText = "Should only a subset of entries in the data folder be imported? Entering a substring here, will cause only entries with the chosen substring to be selected.", Default = null)]
    public string SubString { get; set; }
}