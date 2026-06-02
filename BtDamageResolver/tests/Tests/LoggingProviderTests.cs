using System;
using System.IO;
using AwesomeAssertions;
using Faemiyah.BtDamageResolver.Common.Logging;
using Faemiyah.BtDamageResolver.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Tests verifying that the <see cref="FaemiyahLoggerFactory"/> defers message argument
/// formatting, so that <see cref="object.ToString"/> on a logged argument is only invoked
/// when the log entry actually passes the configured minimum level filter.
/// </summary>
[TestFixture]
internal class LoggingProviderTests
{
    private const string TestCategory = "TestCategory";

    private TextWriter _originalConsoleOut;

    /// <summary>
    /// Redirects console output so the active console provider does not pollute the test runner output.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _originalConsoleOut = Console.Out;
        Console.SetOut(TextWriter.Null);
    }

    /// <summary>
    /// Restores the original console output.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Console.SetOut(_originalConsoleOut);
    }

    /// <summary>
    /// A log call below the configured minimum level must be filtered out before its arguments are
    /// formatted, so <see cref="object.ToString"/> is never invoked on the supplied argument.
    /// </summary>
    [Test]
    public void Log_WhenLevelBelowMinimum_DoesNotInvokeToStringOnArgument()
    {
        // Arrange
        var counter = new ToStringCounter();
        using var factory = CreateFactory(LogLevel.Information);
        var logger = factory.CreateLogger(TestCategory);

        // Act
        logger.LogDebug("Value: {Counter}", counter);

        // Assert
        counter.ToStringInvocations.Should().Be(0);
    }

    /// <summary>
    /// A log call at or above the configured minimum level passes the filter and is formatted,
    /// which invokes <see cref="object.ToString"/> on the supplied argument exactly once.
    /// </summary>
    [Test]
    public void Log_WhenLevelAtOrAboveMinimum_InvokesToStringOnArgument()
    {
        // Arrange
        var counter = new ToStringCounter();
        using var factory = CreateFactory(LogLevel.Information);
        var logger = factory.CreateLogger(TestCategory);

        // Act
        logger.LogInformation("Value: {Counter}", counter);

        // Assert
        counter.ToStringInvocations.Should().Be(1);
    }

    private static FaemiyahLoggerFactory CreateFactory(LogLevel minimumLevel)
    {
        var options = Options.Create(new FaemiyahLoggingOptions
        {
            LogLevel = minimumLevel,
            LogToConsole = true,
            LogToDatabase = false
        });

        return new FaemiyahLoggerFactory(options);
    }

    /// <summary>
    /// Helper whose <see cref="ToString"/> counts how many times it is invoked, used to detect
    /// whether deferred log message formatting reached this argument.
    /// </summary>
    private sealed class ToStringCounter
    {
        public int ToStringInvocations { get; private set; }

        public override string ToString()
        {
            ToStringInvocations++;
            return "counted";
        }
    }
}
