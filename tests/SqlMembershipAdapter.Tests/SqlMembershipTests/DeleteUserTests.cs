using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models.Request;

namespace SqlMembershipAdapter.Tests
{
    public class DeleteUserTests
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
        public void DeleteUser_UsernameValidationFailed_ReturnsParam()
        {
            string failedParam = "Username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.DeleteUser(new DeleteUserRequest()
                {
                    Username = "username",
                    DeleteAllRelatedData = true
                });
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task DeleteUser_NotDeleted_ReturnsFalse()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.DeleteUser(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(false);

            bool result = await _sut.DeleteUser(new DeleteUserRequest()
            {
                Username = "username",
                DeleteAllRelatedData = true
            });

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteUser_IsDeleted_ReturnsTrue()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.DeleteUser(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

            bool result = await _sut.DeleteUser(new DeleteUserRequest()
            {
                Username = "username",
                DeleteAllRelatedData = true
            });

            Assert.That(result, Is.True);
        }
    }
}