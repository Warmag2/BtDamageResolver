using System;
using System.IO;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Verifies the native configuration chain resolves connection strings from the
/// ConnectionStrings block and that environment variables override JSON values,
/// matching the source order appsettings.json -> environment variables.
/// </summary>
[TestFixture]
internal class ConfigurationPrecedenceTests
{
    private const string RedisEnvVariable = "ConnectionStrings__Redis";
    private const string JsonConnectionString = "redis-from-json:6379";

    private const string SampleJson = """
    {
      "ConnectionStrings": {
        "Redis": "redis-from-json:6379"
      }
    }
    """;

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(RedisEnvVariable, null);
    }

    [Test]
    public void GetConnectionString_ResolvesRedisFromJson()
    {
        // Arrange
        var configuration = BuildConfiguration();

        // Act
        var connectionString = configuration.GetConnectionString("Redis");

        // Assert
        connectionString.Should().Be(JsonConnectionString);
    }

    [Test]
    public void EnvironmentVariable_OverridesJsonConnectionString()
    {
        // Arrange
        const string overrideValue = "redis-from-env:6379";
        Environment.SetEnvironmentVariable(RedisEnvVariable, overrideValue);
        var configuration = BuildConfiguration();

        // Act
        var connectionString = configuration.GetConnectionString("Redis");

        // Assert
        connectionString.Should().Be(overrideValue);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)))
            .AddEnvironmentVariables()
            .Build();
    }
}
