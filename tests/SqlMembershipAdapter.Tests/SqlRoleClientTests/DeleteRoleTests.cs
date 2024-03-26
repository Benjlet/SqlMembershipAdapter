using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class DeleteRoleTests
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
        public void DeleteRole_RoleValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleName";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                bool result = await _sut.DeleteRole(role, false);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task DeleteRole_Failed_ReturnsResult()
        {
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.DeleteRole(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(false);

            bool result = await _sut.DeleteRole(role, false);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteRole_Success_ReturnsResult()
        {
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.DeleteRole(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

            bool result = await _sut.DeleteRole(role, false);

            Assert.That(result, Is.True);
        }
    }
}