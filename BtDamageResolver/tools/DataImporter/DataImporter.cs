﻿using System;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter;

/// <summary>
/// The data importer.
/// </summary>
public class DataImporter
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataImporter"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    public DataImporter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Imports data from location specified by options.
    /// </summary>
    /// <param name="options">Data importing options.</param>
    /// <returns>A task which finishes when data importing is completed.</returns>
    public async Task Work(DataImportOptions options)
    {
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        jsonSerializerSettings.Converters.Add(new StringEnumConverter());

        var data = FetchData(options, jsonSerializerSettings);
        _logger.LogInformation("{count} data objects matched the filter(s)", data.Count);

        foreach (var dataObject in data)
        {
            _logger.LogInformation("{object}", JsonConvert.SerializeObject(dataObject, jsonSerializerSettings));
        }

        using var host = await ConnectClient();

        var client = host.Services.GetRequiredService<IClusterClient>();

        if (options.Import)
        {
            foreach (var dataObject in data)
            {
                _logger.LogInformation("Importing Entity {type} {name}", dataObject.GetType(), (dataObject as IEntity<string>)?.GetId());

                switch (dataObject)
                {
                    case Ammo ammo:
                        ammo.FillMissingFields();
                        await client.GetAmmoRepository().AddOrUpdate(ammo);
                        break;
                    case ClusterTable clusterTable:
                        await client.GetClusterTableRepository().AddOrUpdate(clusterTable);
                        break;
                    case CriticalDamageTable criticalDamageTable:
                        await client.GetCriticalDamageTableRepository().AddOrUpdate(criticalDamageTable);
                        break;
                    case PaperDoll paperDoll:
                        await client.GetPaperDollRepository().AddOrUpdate(paperDoll);
                        break;
                    case Unit unit:
                        await client.GetUnitRepository().AddOrUpdate(unit);
                        break;
                    case Weapon weapon:
                        weapon.FillMissingFields();
                        await client.GetWeaponRepository().AddOrUpdate(weapon);
                        break;
                    default:
                        throw new InvalidOperationException($"DataObject is of unknown type: {dataObject.GetType()}");
                }
            }
        }

        _logger.LogInformation("Finished importing.");
    }

    /// <summary>
    /// Process all files in the directory passed in, recurse on any directories that are found, and process the files they contain.
    /// </summary>
    /// <param name="targetDirectory">The target directory.</param>
    /// <param name="searchTerm">The search term for files in the directory.</param>
    /// <param name="jsonSerializerSettings">The JSON serializer settings to use.</param>
    /// <returns>The data objects in the given directory for the search term.</returns>
    private List<object> ProcessDirectory(string targetDirectory, string searchTerm, JsonSerializerSettings jsonSerializerSettings)
    {
        var fileEntries = searchTerm == null
            ? Directory.GetFiles(targetDirectory)
            : Directory.GetFiles(targetDirectory, searchTerm);
        var deserializedEntries = new List<object>();

        foreach (var fileEntry in fileEntries)
        {
            deserializedEntries.AddRange(ProcessFile(fileEntry, jsonSerializerSettings));
        }

        var subDirectories = Directory.GetDirectories(targetDirectory);
        foreach (var directory in subDirectories)
        {
            deserializedEntries.AddRange(ProcessDirectory(directory, searchTerm, jsonSerializerSettings));
        }

        return deserializedEntries;
    }

    private List<object> FetchData(DataImportOptions options, JsonSerializerSettings jsonSerializerSettings)
    {
        var importedDataObjects = new List<object>();

        if (Directory.Exists(options.Folder))
        {
            // This path is a directory
            importedDataObjects.AddRange(ProcessDirectory(options.Folder, options.SubString, jsonSerializerSettings));
        }
        else
        {
            throw new FileNotFoundException("{target} is not a valid directory.");
        }

        return importedDataObjects;
    }

    private async Task<IHost> ConnectClient()
    {
        var configuration = GetConfiguration("DataImporterSettings.json");
        var section = configuration.GetSection(Settings.ClusterOptionsBlockName);
        var clusterOptions = section.Get<FaemiyahClusterOptions>();

        var hostBuilder = new HostBuilder()
            .UseOrleansClient((_, clientBuilder) =>
            {
                clientBuilder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "faemiyah";
                        options.ServiceId = "Resolver";
                    })
                    .Configure<ConnectionOptions>(options =>
                    {
                        options.ConnectionRetryDelay = TimeSpan.FromSeconds(30);
                        options.OpenConnectionTimeout = TimeSpan.FromSeconds(30);
                    }).UseAdoNetClustering(options =>
                    {
                        options.Invariant = clusterOptions?.Invariant;
                        options.ConnectionString = clusterOptions?.ConnectionString;
                    })
                    .Services.AddSerializer(serializerBuilder =>
                    {
                        serializerBuilder.AddNewtonsoftJsonSerializer(isSupported: type => type.Namespace.StartsWith("Faemiyah.BtDamageResolver"));
                    });
            }).Build();

        _logger.LogInformation("Host successfully created.");

        await hostBuilder.StartAsync();

        return hostBuilder;
    }

    /// <summary>
    /// Finds and processes files.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <param name="jsonSerializerSettings">The JSON serializer settings to use.</param>
    /// <returns>A collection of objects found.</returns>
    /// <exception cref="InvalidDataException">If data could not be deserialized to correct type.</exception>
    /// <exception cref="InvalidOperationException">If data type could not be inferred from file name.</exception>
    private IEnumerable<object> ProcessFile(string path, JsonSerializerSettings jsonSerializerSettings)
    {
        var fileNamePrefix = Path.GetFileName(path).Split("_")[0];

        _logger.LogInformation("Loading file: {file}", path);

        var fileData = File.ReadAllText(path, Encoding.UTF8);

        switch (fileNamePrefix)
        {
            case "Ammo":
                return (JsonConvert.DeserializeObject<List<Ammo>>(fileData, jsonSerializerSettings) ?? throw new InvalidDataException("Could not deserialize into list of weapons.")).Select(r => r as object);
            case "ClusterTable":
                return new object[] { JsonConvert.DeserializeObject<ClusterTable>(fileData, jsonSerializerSettings) };
            case "CriticalDamageTable":
                return new object[] { JsonConvert.DeserializeObject<CriticalDamageTable>(fileData, jsonSerializerSettings) };
            case "PaperDoll":
                return new object[] { JsonConvert.DeserializeObject<PaperDoll>(fileData, jsonSerializerSettings) };
            case "Unit":
                return new object[] { JsonConvert.DeserializeObject<Unit>(fileData, jsonSerializerSettings) };
            case "Weapons":
                return (JsonConvert.DeserializeObject<List<Weapon>>(fileData, jsonSerializerSettings) ?? throw new InvalidDataException("Could not deserialize into list of weapons.")).Select(r => r as object);
            default:
                throw new InvalidOperationException($"Cannot infer file type from file name for file: {path}");
        }
    }
}