using System;
using System.Collections.Generic;
using System.IO;
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

namespace Faemiyah.BtDamageResolver.Tools.DataExporter;

/// <summary>
/// The data exporter.
/// </summary>
internal sealed class DataExporter
{
    private readonly ILogger<DataExporter> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RepositoryProvider _repositoryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataExporter"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The application configuration.</param>
    public DataExporter(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<DataExporter>();
        _jsonSerializerOptions = GetJsonSerializerOptions();

        // Pretty printing options
        _jsonSerializerOptions.IndentCharacter = ' ';
        _jsonSerializerOptions.IndentSize = 4;
        _jsonSerializerOptions.WriteIndented = true;

        // Repository provider setup
        var connectionString = configuration.GetConnectionString(Settings.RedisConnectionStringName)
            ?? throw new InvalidOperationException($"No '{Settings.RedisConnectionStringName}' connection string configured.");
        _repositoryProvider = new RepositoryProvider(
            new RedisEntityRepository<Ammo>(loggerFactory.CreateLogger<RedisEntityRepository<Ammo>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<ArcDiagram>(loggerFactory.CreateLogger<RedisEntityRepository<ArcDiagram>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<ClusterTable>(loggerFactory.CreateLogger<RedisEntityRepository<ClusterTable>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<CriticalDamageTable>(loggerFactory.CreateLogger<RedisEntityRepository<CriticalDamageTable>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<PaperDoll>(loggerFactory.CreateLogger<RedisEntityRepository<PaperDoll>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<Unit>(loggerFactory.CreateLogger<RedisEntityRepository<Unit>>(), Options.Create(_jsonSerializerOptions), connectionString),
            new RedisEntityRepository<Weapon>(loggerFactory.CreateLogger<RedisEntityRepository<Weapon>>(), Options.Create(_jsonSerializerOptions), connectionString));
    }

    /// <summary>
    /// Exports data from resolver database.
    /// </summary>
    /// <remarks>
    /// Only units for now.
    /// </remarks>
    /// <param name="options">Data exporting options.</param>
    /// <returns>A task which finishes when data exporting is completed.</returns>
    public async Task Work(DataExportOptions options)
    {
        var data = FetchData();
        _logger.LogInformation("{Count} data objects matched the filter(s)", data.Count);

        foreach (var dataObject in data)
        {
            _logger.LogInformation("{Object}", JsonSerializer.Serialize(dataObject, _jsonSerializerOptions));
        }

        if (!options.DryRun)
        {
            if (!Directory.Exists(options.Folder))
            {
                throw new FileNotFoundException($"{options.Folder} is not a valid directory.");
            }

            foreach (var dataObject in data)
            {
                var dataName = (dataObject as IEntity<string>)?.GetName();

                _logger.LogInformation("Exporting Entity {Type} {Name}", dataObject.GetType(), dataName);

                switch (dataObject)
                {
                    case Unit unit:
                        var filename = Path.Combine(options.Folder, $"Unit_{unit.Type}_{dataName}.json".Replace(' ', '_'));
                        await File.WriteAllTextAsync(filename, JsonSerializer.Serialize(dataObject, _jsonSerializerOptions));
                        break;
                    default:
                        throw new InvalidOperationException($"Exporting of objects of type {dataObject.GetType()} is not supported.");
                }
            }
        }

        _logger.LogInformation("Finished exporting.");
    }

    private List<object> FetchData()
    {
        var exportedDataObjects = new List<object>();

        exportedDataObjects.AddRange(_repositoryProvider.UnitRepository.GetAll());

        return exportedDataObjects;
    }
}
