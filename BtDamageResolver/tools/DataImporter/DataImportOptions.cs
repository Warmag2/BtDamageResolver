using CommandLine;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter
{
    [Verb("import")]
    public class DataImportOptions
    {
        [Option('l', "localhost", Required = false, HelpText = "Use localhost clustering.", Default = false)]
        public bool LocalhostClustering { get; set; }

        [Option('f', "folder", Required = false, HelpText = "Folder to load data from.", Default = "./data/")]
        public string Folder { get; set; }

        [Option('i', "import", Required = false, HelpText = "Import data or not. If not set, will only print the deserialized data.")]
        public bool Import { get; set; }

        [Option('s', "substring", Required = false, HelpText = "Should only a subset of entries in the data folder be imported? Entering a substring here, will cause only entries with the chosen substring to be selected.", Default = null)]
        public string SubString { get; set; }

        [Option('u', "user", Required = false, HelpText = "Server user name.")]
        public string UserName { get; set; }

        [Option('p', "password", Required = false, HelpText = "Server password.")]
        public string Password { get; set; }

        [Option('x', "extra", Required = false, HelpText = "Extra test.")]
        public bool Extra { get; set; }
    }
}
