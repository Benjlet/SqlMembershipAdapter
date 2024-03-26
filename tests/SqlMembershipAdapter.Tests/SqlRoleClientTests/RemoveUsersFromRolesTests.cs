using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class RemoveUsersFromRolesTests
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
        public void RemoveUsersFromRoles_UsernamesValidationFailed_ReturnsParam()
        {
            string? failedParam = "roleNames";

            string[] users = ["user_one", "user_two"];
            string[] roles = ["role_one"];

            _mockValidator.SetupSequence(x => x.ValidateArray(It.IsAny<string[]>()))
              .Returns(false)
              .Returns(true);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.RemoveUsersFromRoles(users, roles);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void RemoveUsersFromRoles_RolesValidationFailed_ReturnsParam()
        {
            string? failedParam = "usernames";

            string[] users = ["user_one", "user_two"];
            string[] roles = ["role_one"];

            _mockValidator.SetupSequence(x => x.ValidateArray(It.IsAny<string[]>()))
              .Returns(true)
              .Returns(false);

            ArgumentException exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _sut.RemoveUsersFromRoles(users, roles);
            });

            Assert.That(exception.Message, Is.EqualTo(failedParam));
        }

        [Test]
        public void RemoveUsersFromRoles_Processed_Completes()
        {
            string[] users = ["user_one", "user_two"];
            string[] roles = ["role_one"];

            _mockValidator.SetupSequence(x => x.ValidateArray(It.IsAny<string[]>()))
              .Returns(true)
              .Returns(true);

            _mockStore.Setup(x => x.RemoveUsersFromRoles(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(Task.CompletedTask);

            async Task Act() => await _sut.RemoveUsersFromRoles(users, roles);
            Assert.DoesNotThrowAsync(Act);
        }
    }
}