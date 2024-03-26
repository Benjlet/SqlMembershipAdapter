using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class FindUsersInRoleTests
    {
        private SqlRoleClient _sut;

        private Mock<ISqlMembershipRoleStore> _mockStore;
        private Mock<ISqlMembershipSettings> _mockSettings;
        private Mock<ISqlMembershipValidator> _mockValidator;

        [SetUp]
        public void Setup()
        {
            _mockStore = new Mock<ISqlMembershipRoleStore>();
            _mockSettings = new Mock<ISqlMembershipSettings>();
            _mockValidator = new Mock<ISqlMembershipValidator>();

            _sut = new SqlRoleClient(
                _mockStore.Object,
                _mockValidator.Object,
                _mockSettings.Object);
        }

        [Test]
        public void FindUsersInRole_UsernameValidationFailed_ReturnsParam()
        {
            string? failedParam = "usernameToMatch";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string[] result = await _sut.FindUsersInRole("role", "");
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void FindUsersInRole_RoleValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleName";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string[] result = await _sut.FindUsersInRole("", "username");
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task FindUsersInRole_Found_ReturnsResult()
        {
            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.FindUsersInRole(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new string[]
            {
                "user_one",
                "user_two"
            });

            string[] result = await _sut.FindUsersInRole(role, username);

            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task FindUsersInRole_NotFound_ReturnsResult()
        {
            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.FindUsersInRole(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            string[] result = await _sut.FindUsersInRole(role, username);

            Assert.That(result, Has.Length.Zero);
        }
    }
}