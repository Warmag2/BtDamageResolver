using FluentAssertions;
using NUnit.Framework;
using static Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver.ExpressionExtensions;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Tests for math expression solver.
/// </summary>
[TestFixture]
public class ExpressionTests
{
    /// <summary>
    /// Test setup.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        // Nothing to be done
    }

    /// <summary>
    /// Test for true token validity.
    /// </summary>
    /// <param name="input">The token to test.</param>
    [Test]
    [TestCase('d')]
    [TestCase('^')]
    [TestCase('/')]
    [TestCase('*')]
    [TestCase('+')]
    [TestCase('-')]
    public void Test_Token_IsAToken_ReturnsTrue(char input)
    {
        // Arrange

        // Act
        var result = input.IsToken();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Test for false token validity.
    /// </summary>
    /// <param name="input">The token to test.</param>
    [Test]
    [TestCase('C')]
    [TestCase('e')]
    [TestCase('i')]
    [TestCase('l')]
    [TestCase('6')]
    [TestCase('0')]
    public void Test_Token_IsNotAToken_ReturnsFalse(char input)
    {
        // Arrange

        // Act
        var result = input.IsToken();

        // Assert
        result.Should().BeFalse();
    }
}