using System;
using System.Collections.Generic;
using System.IO;
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

namespace Faemiyah.BtDamageResolver.Tools.DataExporter;

/// <summary>
/// The data exporter.
/// </summary>
public class DataExporter
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataExporter"/> class.
    /// </summary>
    /// <param name="logger">The logging interface.</param>
    public DataExporter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonSerializerOptions = GetJsonSerializerOptions();

        // Pretty printing options
        _jsonSerializerOptions.IndentCharacter = ' ';
        _jsonSerializerOptions.IndentSize = 4;
        _jsonSerializerOptions.WriteIndented = true;
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
        using var host = await ConnectClient();
        var client = host.Services.GetRequiredService<IClusterClient>();

        var data = await FetchData(client);
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
                var dataName = (dataObject as IEntity<string>)?.GetId();

                _logger.LogInformation("Exporting Entity {Type} {Name}", dataObject.GetType(), dataName);

                switch (dataObject)
                {
                    case Unit unit:
                        var filename = Path.Combine(options.Folder, $"Unit {unit.Type} {dataName}.json".Replace(' ', '_'));
                        await File.WriteAllTextAsync(filename, JsonSerializer.Serialize(dataObject, _jsonSerializerOptions));
                        break;
                    default:
                        throw new InvalidOperationException($"Exporting of objects of type {dataObject.GetType()} is not supported.");
                }
            }
        }

        _logger.LogInformation("Finished exporting.");
    }

    private static async Task<List<object>> FetchData(IClusterClient client)
    {
        var exportedDataObjects = new List<object>();

        exportedDataObjects.AddRange(await client.GetUnitRepository().GetAll());

        return exportedDataObjects;
    }

    private async Task<IHost> ConnectClient()
    {
        var configuration = GetConfiguration("DataExporterSettings.json");
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
}