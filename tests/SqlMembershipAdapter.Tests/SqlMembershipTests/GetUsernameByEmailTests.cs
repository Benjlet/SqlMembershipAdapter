using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class GetUsernameByEmailTests
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
        public void GetUsernameByEmail_EmailValidationFailed_ReturnsParam()
        {
            string failedParam = "email";

            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.GetUserNameByEmail("email");
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task GetUsernameByEmail_SearchesEmail_ReturnsResult()
        {
            string matchedUsername = "username";

            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetUsernameByEmail(It.IsAny<string>())).ReturnsAsync(matchedUsername);

            var username = await _sut.GetUserNameByEmail("email");

            Assert.That(username, Is.EqualTo(matchedUsername));
        }
    }
}