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
        private SqlMembershipClient _sut;

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

            _sut = new SqlMembershipClient(
                _mockStore.Object,
                _mockValidator.Object,
                _mockEncryption.Object,
                _mockSettings.Object);
        }

        [Test]
        public async Task ValidateUser_UsernameValidationFailed_ReturnsFalse()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);

            bool result = await _sut.ValidateUser(new ValidateUserRequest("username", "password"));

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateUser_PasswordValidationFailed_ReturnsFalse()
        {
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(false);

            bool result = await _sut.ValidateUser(new ValidateUserRequest("username", "password"));

            Assert.That(result, Is.False);
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

            bool result = await _sut.ValidateUser(new ValidateUserRequest(username, password));

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

            bool result = await _sut.ValidateUser(new ValidateUserRequest(username, password));

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