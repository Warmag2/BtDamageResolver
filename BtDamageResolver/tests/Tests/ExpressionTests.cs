using FluentAssertions;
using NUnit.Framework;
using static Faemiyah.BtDamageResolver.Actors.Logic.ExpressionSolver.ExpressionExtensions;

namespace Faemiyah.BtDamageResolver.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase('d')]
        [TestCase('^')]
        [TestCase('/')]
        [TestCase('*')]
        [TestCase('+')]
        [TestCase('-')]
        public void Test_Token_IsAToken_ReturnsTrue(char input)
        {
            //Arrange

            //Act
            var result = input.IsToken();

            //Assert
            result.Should().BeTrue();
        }

        [Test]
        [TestCase('C')]
        [TestCase('e')]
        [TestCase('i')]
        [TestCase('l')]
        [TestCase('6')]
        [TestCase('0')]
        public void Test_Token_IsNotAToken_ReturnsFalse(char input)
        {
            //Arrange

            //Act
            var result = input.IsToken();

            //Assert
            result.Should().BeFalse();
        }

    }
}
