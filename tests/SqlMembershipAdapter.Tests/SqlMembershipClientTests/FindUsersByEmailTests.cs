using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;

namespace SqlMembershipAdapter.Tests
{
    public class FindUsersByEmailTests
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
        public void FindUsersByEmail_SearchValidationFailed_ReturnsParam()
        {
            string failedParam = "PageIndex & PageSize";

            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePageRange(It.IsAny<int>(), It.IsAny<int>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.FindUsersByEmail(new FindUsersRequest("email", 3, 5));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void FindUsersByEmail_EmailValidationFailed_ReturnsParam()
        {
            string failedParam = "Criteria";

            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(false);
            _mockValidator.Setup(x => x.ValidatePageRange(It.IsAny<int>(), It.IsAny<int>())).Returns(true);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.FindUsersByEmail(new FindUsersRequest("email", 3, 5));
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task FindUsersByEmail_Searches_ReturnsResult()
        {
            MembershipUser foundUser = CreateMembershipUser();

            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePageRange(It.IsAny<int>(), It.IsAny<int>())).Returns(true);

            _mockStore.Setup(x => x.FindUsersByEmail(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new MembershipUserCollection(
                new List<MembershipUser>()
                {
                    foundUser
                }, 1));

            MembershipUserCollection users = await _sut.FindUsersByEmail(new FindUsersRequest("email", 3, 5));

            Assert.That(users.Count, Is.EqualTo(1));
            Assert.That(users.Users, Has.Count.EqualTo(1));
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