﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataImporter"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    public DataImporter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonSerializerOptions = GetJsonSerializerOptions();
    }

    /// <summary>
    /// Imports data from location specified by options.
    /// </summary>
    /// <param name="options">Data importing options.</param>
    /// <returns>A task which finishes when data importing is completed.</returns>
    public async Task Work(DataImportOptions options)
    {
        var data = FetchData(options);
        _logger.LogInformation("{Count} data objects matched the filter(s)", data.Count);

        foreach (var dataObject in data)
        {
            _logger.LogInformation("{Object}", JsonSerializer.Serialize(dataObject, _jsonSerializerOptions));
        }

        using var host = await ConnectClient();

        var client = host.Services.GetRequiredService<IClusterClient>();

        if (!options.DryRun)
        {
            foreach (var dataObject in data)
            {
                _logger.LogInformation("Importing Entity {Type} {Name}", dataObject.GetType(), (dataObject as IEntity<string>)?.GetId());

                switch (dataObject)
                {
                    case Ammo ammo:
                        ammo.FillMissingFields();
                        await client.GetAmmoRepository().AddOrUpdate(ammo);
                        break;
                    case ArcDiagram arcDiagram:
                        await client.GetArcDiagramRepository().AddOrUpdate(arcDiagram);
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

                        var validationResult = weapon.Validate();
                        if (!validationResult.IsValid)
                        {
                            _logger.LogError("Validation failed for weapon {Weapon}. Reason: {Reason}", weapon.GetId(), string.Join(' ', validationResult.Reasons));
                            throw new InvalidOperationException($"Invalid data in weapon: {weapon.GetId()}");
                        }

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
    /// <returns>The data objects in the given directory for the search term.</returns>
    private List<object> ProcessDirectory(string targetDirectory, string searchTerm)
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
            throw new FileNotFoundException($"{options.Folder} is not a valid directory.");
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
                        serializerBuilder.AddJsonSerializer(isSupported: type => type.Namespace.StartsWith("Faemiyah.BtDamageResolver"));
                    })
                    .ConfigureJsonSerializerOptions();
            }).Build();

        _logger.LogInformation("Host successfully created.");

        await hostBuilder.StartAsync();

        return hostBuilder;
    }

    /// <summary>
    /// Finds and processes files.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <returns>A collection of objects found.</returns>
    /// <exception cref="InvalidDataException">If data could not be deserialized to correct type.</exception>
    /// <exception cref="InvalidOperationException">If data type could not be inferred from file name.</exception>
    private IEnumerable<object> ProcessFile(string path)
    {
        var fileNamePrefix = Path.GetFileName(path).Split("_")[0];

        _logger.LogInformation("Loading file: {FilePath}", path);

        var fileData = File.ReadAllText(path, Encoding.UTF8);

        return fileNamePrefix switch
        {
            "Ammo" => (JsonSerializer.Deserialize<List<Ammo>>(fileData, _jsonSerializerOptions) ?? throw new InvalidDataException("Could not deserialize into a list of ammo.")).Select(r => r as object),
            "ArcDiagram" => [JsonSerializer.Deserialize<ArcDiagram>(fileData, _jsonSerializerOptions) ?? throw new InvalidDataException("Could not deserialize into an arc diagram.")],
            "ClusterTable" => [JsonSerializer.Deserialize<ClusterTable>(fileData, _jsonSerializerOptions)],
            "CriticalDamageTable" => [JsonSerializer.Deserialize<CriticalDamageTable>(fileData, _jsonSerializerOptions)],
            "PaperDoll" => [JsonSerializer.Deserialize<PaperDoll>(fileData, _jsonSerializerOptions)],
            "Unit" => [JsonSerializer.Deserialize<Unit>(fileData, _jsonSerializerOptions)],
            "Weapons" => (JsonSerializer.Deserialize<List<Weapon>>(fileData, _jsonSerializerOptions) ?? throw new InvalidDataException("Could not deserialize into a list of weapons.")).Select(r => r as object),
            _ => throw new InvalidOperationException($"Cannot infer file type from file name for file: {path}"),
        };
    }
}