using CommandLine;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter
{
    /// <summary>
    /// Data importing options.
    /// </summary>
    [Verb("import")]
    public class DataImportOptions
    {
        /// <summary>
        /// Use local host clustering.
        /// </summary>
        [Option('l', "localhost", Required = false, HelpText = "Use localhost clustering.", Default = false)]
        public bool LocalhostClustering { get; set; }

        /// <summary>
        /// The folder to process.
        /// </summary>
        [Option('f', "folder", Required = false, HelpText = "Folder to load data from.", Default = "./data/")]
        public string Folder { get; set; }

        /// <summary>
        /// Import or not. If not set, will produce a dry-run.
        /// </summary>
        [Option('i', "import", Required = false, HelpText = "Import data or not. If not set, will only print the deserialized data.")]
        public bool Import { get; set; }

        /// <summary>
        /// Import only a subset of entries which have this text in their filename.
        /// </summary>
        [Option('s', "substring", Required = false, HelpText = "Should only a subset of entries in the data folder be imported? Entering a substring here, will cause only entries with the chosen substring to be selected.", Default = null)]
        public string SubString { get; set; }
    }
}
