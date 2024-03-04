using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class ChangePasswordTests
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
        public void ChangePassword_ValidationFailed_ReturnsParam()
        {
            string? failedParam = "Password";

            _mockValidator.Setup(x => x.ValidateChangePasswordRequest(It.IsAny<ChangePasswordRequest>(), out failedParam)).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.ChangePassword(new ChangePasswordRequest(
                    "username", "oldPassword", "newPassword"));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void ChangePassword_EncodingFailed_ReturnsParam()
        {
            string? failedParam = "NewPassword";

            _mockValidator.Setup(x => x.ValidateChangePasswordRequest(It.IsAny<ChangePasswordRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(false);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = "hashedOld",
                PasswordFormat = 1,
                PasswordSalt = "salt"
            });

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == "oldPassword"), It.Is<int>(i => i == 1), It.IsAny<string>())).Returns("hashedOld");

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.ChangePassword(new ChangePasswordRequest(
                    "username", "oldPassword", "newPassword"));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task ChangePassword_CheckPasswordFailed_ReturnsFalse()
        {
            string salt = "S4lt";
            string username = "username";
            string oldPassword = "hunter2";
            string newPassword = "hunter3";
            string hashedOldPassword = "Hunt3r2!";
            string hashedNewPassword = "Hunt3r3!";

            string? failedParam = null;

            _mockValidator.Setup(x => x.ValidateChangePasswordRequest(It.IsAny<ChangePasswordRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = hashedOldPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == oldPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedOldPassword);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == newPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedNewPassword);

            bool result = await _sut.ChangePassword(new ChangePasswordRequest(username, oldPassword, newPassword));

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ChangePassword_PasswordChangeFailed_ReturnsFalse()
        {
            string salt = "S4lt";
            string username = "username";
            string oldPassword = "hunter2";
            string newPassword = "hunter3";
            string hashedOldPassword = "Hunt3r2!";
            string hashedNewPassword = "Hunt3r3!";

            string? failedParam = null;

            _mockValidator.Setup(x => x.ValidateChangePasswordRequest(It.IsAny<ChangePasswordRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = hashedOldPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockStore.Setup(x => x.ChangePassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == oldPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedOldPassword);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == newPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedNewPassword);

            bool result = await _sut.ChangePassword(new ChangePasswordRequest(username, oldPassword, newPassword));

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ChangePassword_Success_ReturnsTrue()
        {
            string salt = "S4lt";
            string username = "username";
            string oldPassword = "hunter2";
            string newPassword = "hunter3";
            string hashedOldPassword = "Hunt3r2!";
            string hashedNewPassword = "Hunt3r3!";

            string? failedParam = null;

            _mockValidator.Setup(x => x.ValidateChangePasswordRequest(It.IsAny<ChangePasswordRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = hashedOldPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockStore.Setup(x => x.ChangePassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == oldPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedOldPassword);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == newPassword), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedNewPassword);

            bool result = await _sut.ChangePassword(new ChangePasswordRequest(username, oldPassword, newPassword));

            Assert.That(result, Is.True);
        }
    }
}