using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;

namespace SqlMembershipAdapter.Tests
{
    public class GetNumberOfUsersOnlineTests
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
        public async Task GetNumberOfUsersOnlineTests_Searches_ReturnsCount()
        {
            int userCount = 3;

            _mockStore.Setup(x => x.GetNumberOfUsersOnline(It.IsAny<int>())).ReturnsAsync(userCount);

            int result = await _sut.GetNumberOfUsersOnline(15);

            Assert.That(result, Is.EqualTo(userCount));
        }
    }
}