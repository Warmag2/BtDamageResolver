using CommandLine;

namespace Faemiyah.BtDamageResolver.Tools.DataExporter;

/// <summary>
/// Data exporting options.
/// </summary>
[Verb("export")]
internal sealed class DataExportOptions
{
    /// <summary>
    /// The folder to process.
    /// </summary>
    [Option('f', "folder", Required = false, HelpText = "Folder to export data to.", Default = "./data/")]
    public string Folder { get; set; }

    /// <summary>
    /// Export or not. If set, will produce a dry-run.
    /// </summary>
    [Option('d', "dry-run", Required = false, HelpText = "If set, will only print the deserialized data and not export.", Default = false)]
    public bool DryRun { get; set; }
}