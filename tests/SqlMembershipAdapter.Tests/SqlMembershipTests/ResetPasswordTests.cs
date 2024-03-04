using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class ResetPasswordTests
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
        public void ResetPassword_UsernameValidationFailed_ReturnsParam()
        {
            string? failedParam = "Username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string newPassword = await _sut.ResetPassword(new ResetPasswordRequest()
                {
                    Username = "username",
                    PasswordAnswer = "answer"
                });
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void ResetPassword_PasswordAnswerValidationFailed_ReturnsParam()
        {
            string? failedParam = "PasswordAnswer";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(false);

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

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string newPassword = await _sut.ResetPassword(new ResetPasswordRequest()
                {
                    Username = "username",
                    PasswordAnswer = "answer"
                });
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task ResetPassword_Success_ReturnsNewPassword()
        {
            string newPassword = "hunter2";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(true);

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

            _mockEncryption.Setup(x => x.GeneratePassword()).Returns(newPassword);

            string result = await _sut.ResetPassword(new ResetPasswordRequest()
            {
                Username = "username",
                PasswordAnswer = "answer"
            });

            Assert.That(result, Is.EqualTo(newPassword));
        }
    }
}