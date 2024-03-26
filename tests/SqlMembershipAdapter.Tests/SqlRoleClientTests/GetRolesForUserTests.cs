using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class GetRolesForUserTests
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
        public void GetRolesForUser_UsernameValidationFailed_ReturnsParam()
        {
            string? failedParam = "username";
            string username = "username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                string[] result = await _sut.GetRolesForUser(username);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public async Task GetRolesForUser_Found_ReturnsResult()
        {
            string username = "username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetRolesForUser(It.IsAny<string>())).ReturnsAsync(new string[]
            {
                "role_one",
                "role_two"
            });

            string[] result = await _sut.GetRolesForUser(username);

            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task GetRolesForUser_NotFound_ReturnsResult()
        {
            string username = "username";

            _mockValidator.Setup(x => x.ValidateUsername(It.IsAny<string>())).Returns(true);

            _mockStore.Setup(x => x.GetRolesForUser(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            string[] result = await _sut.GetRolesForUser(username);

            Assert.That(result, Has.Length.Zero);
        }
    }
}