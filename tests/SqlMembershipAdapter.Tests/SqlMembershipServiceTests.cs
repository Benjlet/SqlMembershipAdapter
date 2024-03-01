using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Tests
{
    public class SqlMembershipServiceTests
    {
        private SqlMembershipService _sut;
        
        private Mock<ISqlMembershipStore> _mockStore;
        private Mock<ISqlMembershipSettings> _mockSettings;
        private Mock<ISqlMembershipValidator> _mockValidator;

        [SetUp]
        public void Setup()
        {
            _mockStore = new Mock<ISqlMembershipStore>();
            _mockValidator = new Mock<ISqlMembershipValidator>();
            _mockSettings = new Mock<ISqlMembershipSettings>();

            _mockSettings.SetupGet(x => x.PasswordFormat).Returns(MembershipPasswordFormat.Hashed);

            _mockValidator.Setup(x => x.ValidatePasswordComplexity(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordQuestion(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateEmail(It.IsAny<string>())).Returns(true);

            _sut = new SqlMembershipService(
                _mockStore.Object,
                _mockValidator.Object,
                _mockSettings.Object);
        }

        [Test]
        public async Task CreateUser_Created_ReturnsSuccess()
        {
            var createdUser = CreateMembershipUser();

            _mockStore.Setup(x => x.CreateUser(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
                    .ReturnsAsync(new CreateUserResult(createdUser, MembershipCreateStatus.Success));

            var createUserResult = await _sut.CreateUser(new CreateUserRequest("Test", "hunter2"));

            _mockStore.Verify(x => x.CreateUser(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.Is<string?>(s => s == null), It.IsAny<string?>(), It.IsAny<bool>()), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(MembershipCreateStatus.Success));
                Assert.That(createUserResult.User, Is.Not.Null);
            });
        }

        [Test]
        public async Task CreateUser_WithQuestion_Created_ReturnsSuccess()
        {
            var createdUser = CreateMembershipUser();

            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            _mockStore.Setup(x => x.CreateUser(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
                    .ReturnsAsync(new CreateUserResult(createdUser, MembershipCreateStatus.Success));

            var createUserResult = await _sut.CreateUser(new CreateUserRequest("Test", "hunter2", null, "question", "answer", null, true));

            _mockStore.Verify(x => x.CreateUser(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.Is<string?>(s => s != null), It.IsAny<string?>(), It.IsAny<bool>()), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(MembershipCreateStatus.Success));
                Assert.That(createUserResult.User, Is.Not.Null);
            });
        }

        private static MembershipUser CreateMembershipUser()
        {
            return new MembershipUser(
                providerName: "Provider",
                "UserName",
                Guid.NewGuid(),
                "Email",
                "Question",
                "Comment",
                true,
                false,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                new DateTime(1754, 1, 1));
        }
    }
}