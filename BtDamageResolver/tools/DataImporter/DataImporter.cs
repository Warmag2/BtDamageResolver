using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.ActorInterfaces.Extensions;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter
{
    public class DataImporter
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for device actor importer.
        /// </summary>
        /// <param name="logger">The logging interface.</param>
        public DataImporter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Imports data from location specified by options.
        /// </summary>
        /// <param name="options"></param>
        public async Task Work(DataImportOptions options)
        {
            if (options.Extra)
            {
                /*ExtraTests.MathTests(_logger);
                using (var client = ConnectClient().Result)
                {
                    _logger.LogInformation("Press [Enter] to quit");
                    Console.ReadLine();
                }
                //using var client = ConnectClient(options).Result;

                //var paperDolls = await client.GetPaperDollRepository().GetAllAsync();
                */
                return;
            }

            var data = FetchData(options);
            _logger.LogInformation($"{data.Count} data objects matched the filter(s)");

            foreach (var dataObject in data)
            {
                _logger.LogInformation(JsonConvert.SerializeObject(dataObject));
            }

            if (options.Import)
            {
                await using var client = await ConnectClient(options);
                foreach (var dataObject in data)
                {
                    _logger.LogInformation("Importing Entity {type} {name}", dataObject.GetType(), (dataObject as IEntity<string>)?.GetId());

                    if (dataObject is Ammo ammo)
                    {
                        await client.GetAmmoRepository().AddOrUpdate(ammo);
                    }
                    else if (dataObject is ClusterTable clusterTable)
                    {
                        await client.GetClusterTableRepository().AddOrUpdate(clusterTable);
                    }
                    else if (dataObject is CriticalDamageTable criticalDamageTable)
                    {
                        await client.GetCriticalDamageTableRepository().AddOrUpdate(criticalDamageTable);
                    }
                    else if (dataObject is PaperDoll paperDoll)
                    {
                        await client.GetPaperDollRepository().AddOrUpdate(paperDoll);
                    }
                    else if (dataObject is Unit unitEntry)
                    {
                        await client.GetUnitRepository().AddOrUpdate(unitEntry);
                    }
                    else if (dataObject is Weapon weapon)
                    {
                        weapon.FillMissingFields();
                        await client.GetWeaponRepository().AddOrUpdate(weapon);
                    }
                    else
                    {
                        throw new InvalidOperationException($"DataObject is of unknown type: {dataObject.GetType()}");
                    }
                }
            }

            _logger.LogInformation("Finished importing.");
        }

        private async Task<IClusterClient> ConnectClient(DataImportOptions dataImportOptions)
        {
            var configuration = GetConfiguration("DataImporterSettings.json");
            var section = configuration.GetSection(Settings.ClusterOptionsBlockName);
            var clusterOptions = section.Get<FaemiyahClusterOptions>();

            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "faemiyah";
                    options.ServiceId = "Resolver";
                })
                .Configure<ConnectionOptions>(options =>
                {
                    options.ConnectionRetryDelay = TimeSpan.FromSeconds(30);
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(30);
                });

            if (dataImportOptions.LocalhostClustering)
            {
                client.UseLocalhostClustering();
            }
            else
            {
                client.UseAdoNetClustering(options =>
                {
                    options.Invariant = clusterOptions.Invariant;
                    options.ConnectionString = clusterOptions.ConnectionString;
                });
            }
            var builtClient = client.Build();

            await builtClient.Connect(ex => Task.FromResult(true));
            _logger.LogInformation("Client successfully connected to silo host.");
            return builtClient;
        }

        private List<object> FetchData(DataImportOptions options)
        {
            var importedDataObjects = new List<object>();

            if (Directory.Exists(options.Folder))
            {
                // This path is a directory
                importedDataObjects.AddRange(ProcessDirectory(options.Folder, options.SubString));
            }
            else
            {
                throw new FileNotFoundException("{target} is not a valid directory.");
            }

            return importedDataObjects;
        }

        /// <summary>
        /// Process all files in the directory passed in, recurse on any directories that are found, and process the files they contain.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="searchTerm">The search term for files in the directory.</param>
        public List<object> ProcessDirectory(string targetDirectory, string searchTerm)
        {
            var fileEntries = searchTerm == null
                ? Directory.GetFiles(targetDirectory)
                : Directory.GetFiles(targetDirectory, searchTerm);
            var deserializedEntries = new List<object>();

            foreach (var fileEntry in fileEntries)
            {
                deserializedEntries.AddRange(ProcessFile(fileEntry));   
            }

            var subDirectories = Directory.GetDirectories(targetDirectory);
            foreach (var directory in subDirectories)
            {
                deserializedEntries.AddRange(ProcessDirectory(directory, searchTerm));
            }

            return deserializedEntries;
        }

        // Insert logic for processing found files here.
        private IEnumerable<object> ProcessFile(string path)
        {
            var fileNamePrefix = Path.GetFileName(path).Split("_").First();

            _logger.LogInformation("Loading file: {file}", path);

            var fileData = File.ReadAllText(path, Encoding.UTF8);

            switch (fileNamePrefix)
            {
                case "Ammo":
                    return (JsonConvert.DeserializeObject<List<Ammo>>(fileData) ?? throw new InvalidDataException("Could not deserialize into list of weapons.")).Select(r => r as object);
                case "ClusterTable":
                    return new object[] { JsonConvert.DeserializeObject<ClusterTable>(fileData) };
                case "CriticalDamageTable":
                    return new object[] { JsonConvert.DeserializeObject<CriticalDamageTable>(fileData) };
                case "PaperDoll":
                    return new object[] { JsonConvert.DeserializeObject<PaperDoll>(fileData) };
                case "Unit":
                    return new object[] { JsonConvert.DeserializeObject<Unit>(fileData) };
                case "Weapons":
                    return (JsonConvert.DeserializeObject<List<Weapon>>(fileData) ?? throw new InvalidDataException("Could not deserialize into list of weapons.")).Select(r => r as object);
                default:
                    throw new InvalidOperationException($"Cannot infer file type from file name for file: {path}");
            }
        }
    }
}
