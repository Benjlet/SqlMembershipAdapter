using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class ChangePasswordQuestionAndAnswerTests
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
        public void ChangePasswordQuestionAndAnswer_ValidationFailed_ThrowsArgumentException()
        {
            string username = "username";
            string password = "password";
            string newQuestion = "newQuestion";
            string newAnswer = "newAnswer";

            string? failedParam = "Username";
            _mockValidator.Setup(x => x.ValidateChangePasswordQuestionAnswer(It.IsAny<ChangePasswordQuestionAndAnswerRequest>(), out failedParam)).Returns(false);


            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.ChangePasswordQuestionAndAnswer(new ChangePasswordQuestionAndAnswerRequest(
                    username,
                    password,
                    newQuestion,
                    newAnswer));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task ChangePasswordQuestionAndAnswer_Success_ReturnsTrue()
        {
            string username = "username";
            string password = "password";
            string newQuestion = "newQuestion";
            string newAnswer = "newAnswer";

            string salt = "S4lt";
            string encodedPassword = "3nc0D3dP455W0rd!";

            string? failedParam = "NewPasswordAnswer";

            _mockValidator.Setup(x => x.ValidateChangePasswordQuestionAnswer(It.IsAny<ChangePasswordQuestionAndAnswerRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = encodedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockStore.Setup(x => x.ChangePasswordQuestionAndAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(p => p == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);

            bool result = await _sut.ChangePasswordQuestionAndAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username,
                password,
                newQuestion,
                newAnswer));

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ChangePasswordQuestionAndAnswer_FinalChangeFailed_ReturnsFalse()
        {
            string username = "username";
            string password = "password";
            string newQuestion = "newQuestion";
            string newAnswer = "newAnswer";

            string salt = "S4lt";
            string encodedPassword = "3nc0D3dP455W0rd!";

            string? failedParam = "NewPasswordAnswer";

            _mockValidator.Setup(x => x.ValidateChangePasswordQuestionAnswer(It.IsAny<ChangePasswordQuestionAndAnswerRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = encodedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockStore.Setup(x => x.ChangePasswordQuestionAndAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(p => p == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);

            bool result = await _sut.ChangePasswordQuestionAndAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username,
                password,
                newQuestion,
                newAnswer));

            Assert.That(result, Is.False);
        }

        [Test]
        public void ChangePasswordQuestionAndAnswer_PasswordAnswerInvalid_ThrowsArgumentException()
        {
            string username = "username";
            string password = "password";
            string newQuestion = "newQuestion";
            string newAnswer = "newAnswer";

            string salt = "S4lt";
            string encodedPassword = "3nc0D3dP455W0rd!";

            string? failedParam = "NewPasswordAnswer";

            _mockValidator.Setup(x => x.ValidateChangePasswordQuestionAnswer(It.IsAny<ChangePasswordQuestionAndAnswerRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(false);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = true,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = encodedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            _mockEncryption.Setup(x => x.Encode(It.Is<string>(p => p == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.ChangePasswordQuestionAndAnswer(new ChangePasswordQuestionAndAnswerRequest(
                    username,
                    password,
                    newQuestion,
                    newAnswer));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task ChangePasswordQuestionAndAnswer_CurrentNotRetrieved_ReturnsFalse()
        {
            string username = "username";
            string password = "password";
            string newQuestion = "newQuestion";
            string newAnswer = "newAnswer";

            string salt = "S4lt";
            string encodedPassword = "3nc0D3dP455W0rd!";

            string? failedParam = "NewPasswordAnswer";

            _mockValidator.Setup(x => x.ValidateChangePasswordQuestionAnswer(It.IsAny<ChangePasswordQuestionAndAnswerRequest>(), out failedParam)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(false);

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                FailedPasswordAnswerAttemptCount = 0,
                FailedPasswordAttemptCount = 0,
                IsApproved = true,
                IsRetrieved = false,
                LastActivityDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                Password = encodedPassword,
                PasswordFormat = 1,
                PasswordSalt = salt
            });

            bool result = await _sut.ChangePasswordQuestionAndAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username,
                password,
                newQuestion,
                newAnswer));

            Assert.That(result, Is.False);
        }
    }
}