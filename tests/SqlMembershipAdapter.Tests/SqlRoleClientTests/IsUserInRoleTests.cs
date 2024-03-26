using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class IsUserInRoleTests
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
        public void IsUserInRole_UsernameValidationFailed_ReturnsParam()
        {
            string? failedParam = "username";

            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.IsUserInRole(username, role);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void IsUserInRole_RoleValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleName";

            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(false);
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.IsUserInRole(username, role);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task IsUserInRole_Found_ReturnsResult()
        {
            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.IsUserInRole(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            bool result = await _sut.IsUserInRole(username, role);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsUserInRole_NotFound_ReturnsResult()
        {
            string username = "username";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);
            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.IsUserInRole(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            bool result = await _sut.IsUserInRole(username, role);

            Assert.That(result, Is.False);
        }
    }
}