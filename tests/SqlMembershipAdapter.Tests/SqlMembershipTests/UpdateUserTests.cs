using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Tests
{
    public class UpdateUserTests
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
        public void UpdateUser_UsernameValidationFailed_ReturnsParam()
        {
            MembershipUser user = CreateMembershipUser();
            string failedParam = "UserName";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.UpdateUser(user);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void UpdateUser_EmailValidationFailed_ReturnsParam()
        {
            MembershipUser user = CreateMembershipUser();
            string failedParam = "Email";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.UpdateUser(user);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task UpdateUser_Success_Completes()
        {
            MembershipUser user = CreateMembershipUser();

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(true);

            await _sut.UpdateUser(user);

            _mockStore.Verify(x => x.UpdateUser(It.IsAny<MembershipUser>()), Times.Once());
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