using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class CreateUserTests
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
        public async Task CreateUser_Created_ReturnsSuccess()
        {
            Guid? userId = null;
            string username = "username";
            string email = "email@example.com";
            string salt = "S4lt";
            string password = "hunter2";
            string encodedPassword = "Hunt3r2!";
            string? passwordQuestion = null;
            string? passwordAnswer = null;
            bool isApproved = true;

            MembershipUser createdUser = CreateMembershipUser();

            MembershipCreateStatus status = MembershipCreateStatus.Success;

            _mockSettings.SetupGet(x => x.PasswordFormat).Returns(MembershipPasswordFormat.Hashed);

            _mockValidator.Setup(x => x.ValidateCreateUserRequest(It.IsAny<CreateUserRequest>(), out status)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.Is<string>(s => s == encodedPassword))).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.Is<string>(s => s == null))).Returns(true);

            _mockEncryption.Setup(x => x.GenerateSalt()).Returns(salt);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == null), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(passwordAnswer);

            _mockStore.Setup(x => x.CreateUser(
                It.Is<Guid?>(g => g == userId),
                It.Is<string?>(u => u == username),
                It.Is<string?>(p => p == encodedPassword),
                It.Is<string?>(s => s == salt),
                It.Is<string?>(e => e == email),
                It.Is<string?>(q => q == passwordQuestion),
                It.Is<string?>(a => a == passwordAnswer),
                It.Is<bool>(i => i == isApproved)))
                    .ReturnsAsync(new CreateUserResult(createdUser, MembershipCreateStatus.Success));

            CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));

            _mockStore.Verify(x => x.CreateUser(
                It.Is<Guid?>(g => g == userId),
                It.Is<string?>(u => u == username),
                It.Is<string?>(p => p == encodedPassword),
                It.Is<string?>(s => s == salt),
                It.Is<string?>(e => e == email),
                It.Is<string?>(q => q == passwordQuestion),
                It.Is<string?>(a => a == passwordAnswer),
                It.Is<bool>(i => i == isApproved)), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(MembershipCreateStatus.Success));
                Assert.That(createUserResult.User, Is.Not.Null);
            });
        }

        [Test]
        [TestCase(MembershipCreateStatus.DuplicateEmail)]
        [TestCase(MembershipCreateStatus.DuplicateProviderUserKey)]
        [TestCase(MembershipCreateStatus.DuplicateUserName)]
        [TestCase(MembershipCreateStatus.InvalidAnswer)]
        [TestCase(MembershipCreateStatus.InvalidEmail)]
        [TestCase(MembershipCreateStatus.InvalidPassword)]
        [TestCase(MembershipCreateStatus.InvalidProviderUserKey)]
        [TestCase(MembershipCreateStatus.InvalidQuestion)]
        [TestCase(MembershipCreateStatus.InvalidUserName)]
        [TestCase(MembershipCreateStatus.ProviderError)]
        [TestCase(MembershipCreateStatus.UserRejected)]
        public async Task CreateUser_ValidationFailed_ReturnsStatus(MembershipCreateStatus status)
        {
            Guid? userId = null;
            string username = "username";
            string email = "email@example.com";
            string password = "hunter2";
            string? passwordQuestion = null;
            string? passwordAnswer = null;
            bool isApproved = true;

            _mockValidator.Setup(x => x.ValidateCreateUserRequest(It.IsAny<CreateUserRequest>(), out status)).Returns(false);

            CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(status));
                Assert.That(createUserResult.User, Is.Null);
            });
        }

        [Test]
        public async Task CreateUser_HashingFailed_ReturnsInvalidPassword()
        {
            Guid? userId = null;
            string username = "username";
            string email = "email@example.com";
            string salt = string.Empty;
            string password = "hunter2";
            string encodedPassword = "Hunt3r2!";
            string? passwordQuestion = null;
            string? passwordAnswer = null;
            bool isApproved = true;

            MembershipUser createdUser = CreateMembershipUser();

            MembershipCreateStatus status = MembershipCreateStatus.Success;

            _mockSettings.SetupGet(x => x.PasswordFormat).Returns(MembershipPasswordFormat.Hashed);

            _mockValidator.Setup(x => x.ValidateCreateUserRequest(It.IsAny<CreateUserRequest>(), out status)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.Is<string>(s => s == encodedPassword))).Returns(false);

            _mockEncryption.Setup(x => x.GenerateSalt()).Returns(salt);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);

            CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(MembershipCreateStatus.InvalidPassword));
                Assert.That(createUserResult.User, Is.Null);
            });
        }

        [Test]
        public async Task CreateUser_AnswerHashingFailed_ReturnsInvalidAnswer()
        {
            Guid? userId = null;
            string username = "username";
            string email = "email@example.com";
            string salt = string.Empty;
            string password = "hunter2";
            string encodedPassword = "Hunt3r2!";
            string? passwordQuestion = null;
            string? passwordAnswer = null;
            bool isApproved = true;

            MembershipUser createdUser = CreateMembershipUser();

            MembershipCreateStatus status = MembershipCreateStatus.Success;

            _mockSettings.SetupGet(x => x.PasswordFormat).Returns(MembershipPasswordFormat.Hashed);

            _mockValidator.Setup(x => x.ValidateCreateUserRequest(It.IsAny<CreateUserRequest>(), out status)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.Is<string>(s => s == encodedPassword))).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.Is<string>(s => s == null))).Returns(false);

            _mockEncryption.Setup(x => x.GenerateSalt()).Returns(salt);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);

            CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(MembershipCreateStatus.InvalidAnswer));
                Assert.That(createUserResult.User, Is.Null);
            });
        }

        [Test]
        [TestCase(MembershipCreateStatus.DuplicateEmail)]
        [TestCase(MembershipCreateStatus.DuplicateProviderUserKey)]
        [TestCase(MembershipCreateStatus.DuplicateUserName)]
        [TestCase(MembershipCreateStatus.InvalidAnswer)]
        [TestCase(MembershipCreateStatus.InvalidEmail)]
        [TestCase(MembershipCreateStatus.InvalidPassword)]
        [TestCase(MembershipCreateStatus.InvalidProviderUserKey)]
        [TestCase(MembershipCreateStatus.InvalidQuestion)]
        [TestCase(MembershipCreateStatus.InvalidUserName)]
        [TestCase(MembershipCreateStatus.ProviderError)]
        [TestCase(MembershipCreateStatus.UserRejected)]
        public async Task CreateUser_CreationFailed_ReturnsStatus(MembershipCreateStatus status)
        {
            Guid? userId = null;
            string username = "username";
            string email = "email@example.com";
            string salt = "S4lt";
            string password = "hunter2";
            string encodedPassword = "Hunt3r2!";
            string? passwordQuestion = null;
            string? passwordAnswer = null;
            bool isApproved = true;

            MembershipCreateStatus validationStatus = MembershipCreateStatus.Success;

            _mockSettings.SetupGet(x => x.PasswordFormat).Returns(MembershipPasswordFormat.Hashed);

            _mockValidator.Setup(x => x.ValidateCreateUserRequest(It.IsAny<CreateUserRequest>(), out validationStatus)).Returns(true);
            _mockValidator.Setup(x => x.ValidatePassword(It.Is<string>(s => s == encodedPassword))).Returns(true);
            _mockValidator.Setup(x => x.ValidatePasswordAnswer(It.Is<string>(s => s == null))).Returns(true);

            _mockEncryption.Setup(x => x.GenerateSalt()).Returns(salt);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == password), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(encodedPassword);
            _mockEncryption.Setup(x => x.Encode(It.Is<string>(s => s == null), It.Is<int>(i => i == 1), It.Is<string>(s => s == salt))).Returns(passwordAnswer);

            _mockStore.Setup(x => x.CreateUser(
                It.Is<Guid?>(g => g == userId),
                It.Is<string?>(u => u == username),
                It.Is<string?>(p => p == encodedPassword),
                It.Is<string?>(s => s == salt),
                It.Is<string?>(e => e == email),
                It.Is<string?>(q => q == passwordQuestion),
                It.Is<string?>(a => a == passwordAnswer),
                It.Is<bool>(i => i == isApproved)))
                    .ReturnsAsync(new CreateUserResult(status));

            CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));

            _mockStore.Verify(x => x.CreateUser(
                It.Is<Guid?>(g => g == userId),
                It.Is<string?>(u => u == username),
                It.Is<string?>(p => p == encodedPassword),
                It.Is<string?>(s => s == salt),
                It.Is<string?>(e => e == email),
                It.Is<string?>(q => q == passwordQuestion),
                It.Is<string?>(a => a == passwordAnswer),
                It.Is<bool>(i => i == isApproved)), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(createUserResult.Status, Is.EqualTo(status));
                Assert.That(createUserResult.User, Is.Null);
            });
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