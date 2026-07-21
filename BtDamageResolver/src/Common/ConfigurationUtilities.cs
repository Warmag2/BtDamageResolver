using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Faemiyah.BtDamageResolver.Common;

/// <summary>
/// Static methods for configuration handling.
/// </summary>
public static class ConfigurationUtilities
{
    private const string EnvironmentOrleansClientPort = "ORLEANS_CLIENTPORT";
    private const string EnvironmentOrleansSiloPort = "ORLEANS_SILOPORT";
    private const int DefaultOrleansClientPort = 30000;
    private const int DefaultOrleansSiloPort = 11111;

    /// <summary>
    /// Get the ports for the silo from environment variables.
    /// </summary>
    /// <returns>A tuple containing the client and silo ports.</returns>
    public static (int ClientPort, int SiloPort) GetSiloPortConfigurationFromEnvironment()
    {
        int clientPort = DefaultOrleansClientPort, siloPort = DefaultOrleansSiloPort;

        try
        {
            var envClientPort = Environment.GetEnvironmentVariable(EnvironmentOrleansClientPort);
            var envSiloPort = Environment.GetEnvironmentVariable(EnvironmentOrleansSiloPort);

            if (envClientPort != null && envSiloPort != null)
            {
                clientPort = int.Parse(envClientPort, NumberStyles.Integer, CultureInfo.InvariantCulture);
                siloPort = int.Parse(envSiloPort, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to resolve silo port configuration from environment variables.", ex);
        }

        return (clientPort, siloPort);
    }

    /// <summary>
    /// Gets the first non-loopback IPv4 address of the current computer. Should work also in Docker/Kubernetes.
    /// </summary>
    /// <returns>The IP address of the computer in the local network.</returns>
    public static IPAddress GetHostIp()
    {
        var dnsEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var hostEntry in dnsEntry.AddressList)
        {
            if (hostEntry.AddressFamily == AddressFamily.InterNetwork)
            {
                return new IPAddress(hostEntry.GetAddressBytes());
            }
        }

        throw new InvalidOperationException("Could not resolve an IP address for this host.");
    }

    /// <summary>
    /// Gets the default JSON Serializer options.
    /// </summary>
    /// <returns>The default JSON serializer options.</returns>
    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false
        };

        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        return jsonSerializerOptions;
    }

    /// <summary>
    /// Add the default JSON serializer options to the service collection.
    /// </summary>
    /// <param name="collection">The service collection.</param>
    /// <returns>The service collection for further processing.</returns>
    public static IServiceCollection ConfigureJsonSerializerOptions(this IServiceCollection collection)
    {
        return collection.Configure<JsonSerializerOptions>(ApplyDefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Build a new <see cref="JsonSerializerOptions"/> instance with the Faemiyah defaults applied.
    /// </summary>
    /// <returns>The configured options instance.</returns>
    public static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        ApplyDefaultJsonSerializerOptions(options);
        return options;
    }

    private static void ApplyDefaultJsonSerializerOptions(JsonSerializerOptions options)
    {
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNameCaseInsensitive = false;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}