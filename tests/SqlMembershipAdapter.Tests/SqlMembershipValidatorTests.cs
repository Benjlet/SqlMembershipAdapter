using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class SqlMembershipValidatorTests
    {
        private Mock<ISqlMembershipSettings> _mockSettings;
        private SqlMembershipValidator _sut;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISqlMembershipSettings>();

            _sut = new SqlMembershipValidator(_mockSettings.Object);
        }

        [Test]
        public void ValidateUsername_Comma_ReturnsFalse()
        {
            var isValid = _sut.ValidateUsername("Username,");

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_TooLong_ReturnsFalse()
        {
            var isValid = _sut.ValidateUsername(new string('A', 400));

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Null_ReturnsFalse()
        {
            var isValid = _sut.ValidateUsername(null);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Empty_ReturnsFalse()
        {
            var isValid = _sut.ValidateUsername(string.Empty);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Valid_ReturnsTrue()
        {
            var isValid = _sut.ValidateUsername("Username");

            Assert.That(isValid, Is.True);
        }
    }
}