using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class ValidateUserTests
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
        public void ValidateUser_UsernameValidationFailed_ReturnsParam()
        {
            string failedParam = "Username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.ValidateUser(new ValidateUserRequest()
                {
                    Password = "password",
                    Username = "username"
                });
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void ValidateUser_PasswordValidationFailed_ReturnsParam()
        {
            string failedParam = "Password";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.ValidateUser(new ValidateUserRequest()
                {
                    Password = "password",
                    Username = "username"
                });
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task ValidateUser_HashNotMatched_ReturnsFalse()
        {
            string username = "username";
            string password = "hunter2";
            string hashedPassword = "Hunt3r2!";
            string salt = "S4lt";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = hashedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns("A different password");

            bool result = await _sut.ValidateUser(new ValidateUserRequest()
            {
                Username = username,
                Password = password
            });

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateUser_Success_ReturnsTrue()
        {
            string username = "username";
            string password = "hunter2";
            string hashedPassword = "Hunt3r2!";
            string salt = "S4lt";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = hashedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(hashedPassword);

            bool result = await _sut.ValidateUser(new ValidateUserRequest()
            {
                Username = username,
                Password = password
            });

            Assert.That(result, Is.True);
        }

        private static MembershipUser CreateMembershipUser(
            string? userName = "UserName",
            Guid? guid = null,
            string? email = "email@example.com",
            string? question = "question?",
            bool isApproved = true,
            bool isLockedOut = false)
        {
            return new MembershipUser(
                providerName: "Provider",
                userName: userName,
                providerUserKey: guid ?? Guid.NewGuid(),
                email: email,
                passwordQuestion: question,
                comment: null,
                isApproved: isApproved,
                isLockedOut: isLockedOut,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                new DateTime(1754, 1, 1));
        }
    }
}