using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Tests
{
    public class GetPasswordTests
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
        public async Task GetPasswordData_DataPresent_ReturnsPasswordData()
        {
            int passwordFormat = 1;
            string password = "hunter2";
            string salt = "S4lt";

            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), false)).ReturnsAsync(new GetPasswordWithFormatResult()
            {
                Password = password,
                PasswordSalt = salt,
                PasswordFormat = passwordFormat
            });

            PasswordData passwordData = await _sut.GetPassword("Username");

            _mockStore.Verify(x => x.GetPasswordWithFormat(It.IsAny<string>(), false), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(passwordData.Password, Is.EqualTo(password));
                Assert.That(passwordData.PasswordSalt, Is.EqualTo(salt));
                Assert.That(passwordData.PasswordFormatCode, Is.EqualTo(passwordFormat));
            });
        }

        [Test]
        public async Task GetPasswordData_NoData_ReturnsEmptyPasswordData()
        {
            _mockStore.Setup(x => x.GetPasswordWithFormat(It.IsAny<string>(), false)).ReturnsAsync(new GetPasswordWithFormatResult());

            PasswordData passwordData = await _sut.GetPassword("Username");

            _mockStore.Verify(x => x.GetPasswordWithFormat(It.IsAny<string>(), false), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(passwordData.Password, Is.Null);
                Assert.That(passwordData.PasswordSalt, Is.Null);
                Assert.That(passwordData.PasswordFormatCode, Is.Zero);
            });
        }
    }
}