using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class CreateRoleTests
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
        public void CreateRole_RoleValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleName";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.CreateRole(role);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void CreateRole_Processed_Completes()
        {
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.CreateRole(It.IsAny<string>())).Returns(Task.CompletedTask);

            async Task Act() => await _sut.CreateRole(role);
            Assert.DoesNotThrowAsync(Act);
        }
    }
}