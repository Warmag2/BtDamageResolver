using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories;
using Faemiyah.BtDamageResolver.Api.ClientInterface.Repositories.Providers;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities;
using Faemiyah.BtDamageResolver.Common.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Faemiyah.BtDamageResolver.Common.ConfigurationUtilities;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter;

/// <summary>
/// The data importer.
/// </summary>
internal sealed class DataImporter
{
    private readonly ILogger<DataImporter> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RepositoryProvider _repositoryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataImporter"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The application configuration.</param>
    public DataImporter(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<DataImporter>();
        _jsonSerializerOptions = GetJsonSerializerOptions();

        // Repository provider setup
        var redisConnectionString = configuration.GetConnectionString(Settings.RedisConnectionStringName);
        _repositoryProvider = new RepositoryProvider(
            new RedisEntityRepository<Ammo>(loggerFactory.CreateLogger<RedisEntityRepository<Ammo>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ArcDiagram>(loggerFactory.CreateLogger<RedisEntityRepository<ArcDiagram>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<ClusterTable>(loggerFactory.CreateLogger<RedisEntityRepository<ClusterTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<CriticalDamageTable>(loggerFactory.CreateLogger<RedisEntityRepository<CriticalDamageTable>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<PaperDoll>(loggerFactory.CreateLogger<RedisEntityRepository<PaperDoll>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Unit>(loggerFactory.CreateLogger<RedisEntityRepository<Unit>>(), Options.Create(_jsonSerializerOptions), redisConnectionString),
            new RedisEntityRepository<Weapon>(loggerFactory.CreateLogger<RedisEntityRepository<Weapon>>(), Options.Create(_jsonSerializerOptions), redisConnectionString));
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

        if (!options.DryRun)
        {
            foreach (var dataObject in data)
            {
                _logger.LogInformation("Importing Entity {Type} {Name}", dataObject.GetType(), (dataObject as IEntity<string>)?.GetName());

                switch (dataObject)
                {
                    case Ammo ammo:
                        ammo.FillMissingFields();
                        await _repositoryProvider.AmmoRepository.AddOrUpdateAsync(ammo);
                        break;
                    case ArcDiagram arcDiagram:
                        await _repositoryProvider.ArcDiagramRepository.AddOrUpdateAsync(arcDiagram);
                        break;
                    case ClusterTable clusterTable:
                        await _repositoryProvider.ClusterTableRepository.AddOrUpdateAsync(clusterTable);
                        break;
                    case CriticalDamageTable criticalDamageTable:
                        await _repositoryProvider.CriticalDamageTableRepository.AddOrUpdateAsync(criticalDamageTable);
                        break;
                    case PaperDoll paperDoll:
                        await _repositoryProvider.PaperDollRepository.AddOrUpdateAsync(paperDoll);
                        break;
                    case Unit unit:
                        await _repositoryProvider.UnitRepository.AddOrUpdateAsync(unit);
                        break;
                    case Weapon weapon:
                        weapon.FillMissingFields();

                        var validationResult = weapon.Validate();
                        if (!validationResult.IsValid)
                        {
                            _logger.LogError("Validation failed for weapon {Weapon}. Reason: {Reason}", weapon.GetName(), string.Join(' ', validationResult.Reasons));
                            throw new InvalidOperationException($"Invalid data in weapon: {weapon.GetName()}");
                        }

                        await _repositoryProvider.WeaponRepository.AddOrUpdateAsync(weapon);
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
