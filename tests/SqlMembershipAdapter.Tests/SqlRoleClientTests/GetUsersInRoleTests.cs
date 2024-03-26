using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class GetUsersInRoleTests
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
        public void GetUsersInRole_RoleValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleName";
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string[] result = await _sut.GetUsersInRole(role);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task GetUsersInRole_Found_ReturnsResult()
        {
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetUsersInRole(It.IsAny<string>())).ReturnsAsync(new string[]
            {
                "user_one",
                "user_two"
            });

            string[] result = await _sut.GetUsersInRole(role);

            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task GetUsersInRole_NotFound_ReturnsResult()
        {
            string role = "admin";

            _mockValidator.Setup(x => x.ValidateRoleName(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetUsersInRole(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            string[] result = await _sut.GetUsersInRole(role);
            Assert.That(result, Has.Length.Zero);
        }
    }
}