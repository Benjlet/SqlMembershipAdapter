using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Tests
{
    public class GetUserTests
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
        public void GetUserTests_UsernameValidationFailed_ReturnsParam()
        {
            string failedParam = "username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.GetUser("username", updateLastActivity: true);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task GetUserTests_UserFound_ReturnsUser()
        {
            MembershipUser user = CreateMembershipUser();

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetUser(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(user);

            MembershipUser? result = await _sut.GetUser("username", updateLastActivity: true);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetUserTests_UserNotFound_UserNotReturned()
        {
            MembershipUser? user = null;

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetUser(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(user);

            MembershipUser? result = await _sut.GetUser("username", updateLastActivity: true);

            Assert.That(result, Is.Null);
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