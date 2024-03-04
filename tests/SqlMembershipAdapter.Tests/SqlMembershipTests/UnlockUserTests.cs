using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class UnlockUserTests
    {
        private SqlMembership _sut;

        private Mock<ISqlMembershipStore> _mockStore;
        private Mock<ISqlMembershipSettings> _mockSettings;
        private Mock<ISqlMembershipValidator> _mockValidator;
        private Mock<ISqlMembershipEncryption> _mockEncryption;

        [SetUp]
        public void Setup()
        {
            _mockStore = new Mock<ISqlMembershipStore>();
            _mockSettings = new Mock<ISqlMembershipSettings>();
            _mockValidator = new Mock<ISqlMembershipValidator>();
            _mockEncryption = new Mock<ISqlMembershipEncryption>();

            _sut = new SqlMembership(
                _mockStore.Object,
                _mockValidator.Object,
                _mockEncryption.Object,
                _mockSettings.Object);
        }

        [Test]
        public void UnlockUserTests_UsernameValidationFailed_ReturnsParam()
        {
            string failedParam = "username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.UnlockUser("username");
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task UnlockUserTests_UnlockFailed_ReturnsFalse()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.UnlockUser(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _sut.UnlockUser("username");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UnlockUserTests_UnlockSuccess_ReturnsTrue()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.UnlockUser(It.IsAny<string>())).ReturnsAsync(true);

            var result = await _sut.UnlockUser("username");

            Assert.That(result, Is.True);
        }
    }
}