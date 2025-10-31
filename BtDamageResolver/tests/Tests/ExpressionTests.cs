using System;
using AwesomeAssertions;
using Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver;
using Faemiyah.BtDamageResolver.Api;
using NUnit.Framework;
using static Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver.ExpressionExtensions;

namespace Faemiyah.BtDamageResolver.Tests;

/// <summary>
/// Tests for math expression solver.
/// </summary>
[TestFixture]
internal class ExpressionTests
{
    private const decimal Epsilon = 0.000000000001m;

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

    /// <summary>
    /// Test for expression correctness.
    /// </summary>
    /// <param name="input">The input expression.</param>
    /// <param name="output">The correct result.</param>
    [Test]
    [TestCase("6", "6")]
    [TestCase("6+5", "11")]
    [TestCase("6*5+4", "34")]
    [TestCase("6*4/60", "0.4")]
    [TestCase("4/60*6", "0.4")]
    [TestCase("(9*4)/60", "0.6")]
    [TestCase("Ceil((9*4)/60)", "1")]
    [TestCase("Floor((9*4)/60)", "0")]
    [TestCase("5+7+2^8+3+11", "282")]
    [TestCase("7/2^3*3+11-1", "12.625")]
    [TestCase("7/2^3*3-1+11", "12.625")]
    [TestCase("3-7/2*2/7+1", "3")]
    [TestCase("3^3-2*2*3-4^2", "-1")]
    public void ReturnsCorrectResult(string input, decimal output)
    {
        // Arrange
        var random = new ResolverRandom();
        var expression = new Expression(random, input);

        // Act
        var result = expression.Parse();

        // Assert
        Math.Abs(result - output).Should().BeLessThanOrEqualTo(Epsilon);
    }
}