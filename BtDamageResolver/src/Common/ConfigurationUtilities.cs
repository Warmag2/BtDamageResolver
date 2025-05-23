using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Filters;

namespace Faemiyah.BtDamageResolver.Common;

/// <summary>
/// Static methods for configuration handling.
/// </summary>
public static class ConfigurationUtilities
{
    private const string EnvironmentOrleansClientPort = "ORLEANS_CLIENTPORT";
    private const string EnvironmentOrleansSiloPort = "ORLEANS_SILOPORT";
    private const string EnvironmentName = "RESOLVER_ENVIRONMENT";
    private const int DefaultOrleansClientPort = 30000;
    private const int DefaultOrleansSiloPort = 11111;
    private const string DefaultDebugEnvironmentName = "Debug";
    private const string DefaultEnvironmentName = DefaultDebugEnvironmentName;

    /// <summary>
    /// Get the ports for the silo from environment variables.
    /// Additionally, if the application is being run in debug mode and multiple silos are active
    /// on the same machine, the ports can be padded by a number so, that one can run multiple silos
    /// without encountering a port conflict.
    /// </summary>
    /// <param name="portPad">Number to add to all ports if debug environment is active. In release environments, this parameter does nothing.</param>
    /// <returns>A tuple containing the client and silo ports.</returns>
    public static (int ClientPort, int SiloPort) GetSiloPortConfigurationFromEnvironment(int portPad = 0)
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
            throw new ConfigurationErrorsException("Unable to resolve silo port configuration from environment variables.", ex);
        }

        if (GetEnvironmentName() == DefaultDebugEnvironmentName)
        {
            clientPort += portPad;
            siloPort += portPad;
        }

        return (clientPort, siloPort);
    }

    /// <summary>
    /// Gets the environment name of the system from an environment variable.
    /// </summary>
    /// <returns>The state of the ThorCore Environment variable.</returns>
    public static string GetEnvironmentName()
    {
        try
        {
            return Environment.GetEnvironmentVariable(EnvironmentName);
        }
        catch (Exception ex)
        {
            throw new ConfigurationErrorsException($"Unable to resolve environment name from environment variables, using default environment name: {DefaultEnvironmentName}).", ex);
        }
    }

    /// <summary>
    /// Gets the configuration from disk.
    /// This method respects the release status of the silo, and will load the appropriate configuration
    /// override file in addition to the default configuration file.
    /// </summary>
    /// <param name="settingsFile">File name of the configuration file.</param>
    /// <returns>An <see cref="IConfiguration"/> object containing the program configuration.</returns>
    /// <throws cref="ConfigurationException">If building the <seealso cref="IConfiguration"/> object is unsuccessful.</throws>
    public static IConfiguration GetConfiguration(string settingsFile)
    {
        try
        {
            var environmentName = GetEnvironmentName();
            var environmentOverrideFile = settingsFile.Replace(".json", $".{environmentName}.json");

            ConfigurationBuilder config = new();
            config.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(settingsFile);

            if (environmentName != null)
            {
                if (File.Exists(environmentOverrideFile))
                {
                    config.AddJsonFile(environmentOverrideFile);
                }
                else
                {
                    throw new ConfigurationErrorsException($"No configuration override file present for given environment name: {environmentName}");
                }
            }

            config.AddEnvironmentVariables();

            return config.Build();
        }
        catch (Exception ex)
        {
            throw new ConfigurationErrorsException("Unable to build configuration.", ex);
        }
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

        throw new ConfigurationErrorsException("Could not resolve an IP address for this host.");
    }

    /// <summary>
    /// Initializes the logging system.
    /// </summary>
    /// <param name="options">The logging options.</param>
    /// <returns>A logging interface.</returns>
    public static ILogger InitializeLogging(FaemiyahLoggingOptions options)
    {
        var logfile = options.LogFile;
        var logLevel = options.LogLevel;
        var logLevelOrleans = options.LogLevelOrleans;

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .Enrich.With(new PropertyEnricher("ProgramName", options.ProgramName))
            .Filter.ByExcluding(e => Matching.FromSource("Orleans").Invoke(e) && e.Level < logLevelOrleans);

        if (options.LogToConsole)
        {
            logger.WriteTo.Console(logLevel);
        }

        if (options.LogToFile)
        {
            logger.WriteTo.File(logfile, logLevel, rollingInterval: RollingInterval.Day, shared: true);
        }

        return logger.CreateLogger();
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
        return collection.Configure<JsonSerializerOptions>(options =>
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNameCaseInsensitive = false;

            options.Converters.Add(new JsonStringEnumConverter());
        });
    }
}