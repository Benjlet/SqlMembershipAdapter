using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using System.Text.RegularExpressions;

namespace SqlMembershipAdapter.Tests
{
    public class SqlMembershipEncryptionTests
    {
        private const string TextForEncryption = "TextForEncryption";
        private const string SaltForEncryption = "hJxc4wwxfp7DrJNuP8IscA==";

        private const string ExpectedEncodedMD5 = "0P7nvutw8a4pn/xlioe0dA==";
        private const string ExpectedEncodedSHA1 = "Tce9itFwkX45k8B5W6Ua/ssrbZ4=";
        private const string ExpectedEncodedSHA256 = "vJh6o+cZKaT8DIyuxUJPcP2a7q0eu4F1DfcZLdEr6aA=";
        private const string ExpectedEncodedSHA384 = "RaWvZfY2oG3dGbnaJYxpml/+390HdRRzPPS70qUxBS8Ejbjeh78zBWmad9tYOEtg";
        private const string ExpectedEncodedSHA512 = "e7+DREUe8AwDfOEvXx2QZUASB4kAnO+uN1Qm9GCgU+7YJaSV6qcahRkFQ5gBEPSZ0F5GgioRju7h+UgrROVCzQ==";

        private SqlMembershipEncryption _sut;
        
        private Mock<ISqlMembershipSettings> _mockSettings;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISqlMembershipSettings>();

            _sut = new SqlMembershipEncryption(_mockSettings.Object);
        }

        [Test]
        public void GenerateSalt_GeneratesValue()
        {
            string salt = _sut.GenerateSalt();

            Assert.That(!string.IsNullOrEmpty(salt), Is.True);
        }

        [Test]
        public void GeneratePassword_GeneratesValue()
        {
            string password = _sut.GeneratePassword();

            Assert.That(!string.IsNullOrEmpty(password), Is.True);
        }

        [Test]
        public void GeneratePassword_MinRequiredTwenty_GeneratesPassword_LengthTwenty()
        {
            _mockSettings.SetupGet(x => x.MinRequiredPasswordLength).Returns(20);

            string password = _sut.GeneratePassword();

            Assert.That(password, Has.Length.EqualTo(20));
        }

        [Test]
        public void GeneratePassword_MinTwelveNonAlpha_GeneratesPassword_AtLeastTwelveNonAlpha()
        {
            _mockSettings.SetupGet(x => x.MinRequiredPasswordLength).Returns(20);
            _mockSettings.SetupGet(x => x.MinRequiredNonAlphanumericCharacters).Returns(12);

            string password = _sut.GeneratePassword();

            string pattern = @"[^a-zA-Z0-9]";
            int count = Regex.Matches(password, pattern).Count;

            Assert.That(count, Is.GreaterThanOrEqualTo(12));
        }

        [Test]
        public void Encode_Hashed_NoAlgorithmSpecified_ReturnsEncoded_SHA1()
        {
            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedSHA1));
        }

        [Test]
        public void Encode_Hashed_SHA1_ReturnsEncoded_SHA1()
        {
            _mockSettings.SetupGet(x => x.HashAlgorithm).Returns(HashAlgorithmType.SHA1);

            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedSHA1));
        }

        [Test]
        public void Encode_Hashed_MD5_ReturnsEncoded_MD5()
        {
            _mockSettings.SetupGet(x => x.HashAlgorithm).Returns(HashAlgorithmType.MD5);

            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedMD5));
        }

        [Test]
        public void Encode_Hashed_SHA256_ReturnsEncoded_SHA256()
        {
            _mockSettings.SetupGet(x => x.HashAlgorithm).Returns(HashAlgorithmType.SHA256);

            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedSHA256));
        }

        [Test]
        public void Encode_Hashed_SHA384_ReturnsEncoded_SHA384()
        {
            _mockSettings.SetupGet(x => x.HashAlgorithm).Returns(HashAlgorithmType.SHA384);

            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedSHA384));
        }

        [Test]
        public void Encode_Hashed_SHA512_ReturnsEncoded_SHA512()
        {
            _mockSettings.SetupGet(x => x.HashAlgorithm).Returns(HashAlgorithmType.SHA512);

            string text = TextForEncryption;
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(ExpectedEncodedSHA512));
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("text")]
        public void Encode_Clear_ReturnsAsProvided(string text)
        {
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Clear;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(text));
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("text")]
        public void Encode_Encrypted_ReturnsAsProvided(string text)
        {
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Encrypted;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(text));
        }

        [Test]
        public void Encode_EmptySalt_ReturnsAsProvided()
        {
            string text = TextForEncryption;
            string salt = "";
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(text));
        }

        [Test]
        public void Encode_EmptyText_ReturnsAsProvided()
        {
            string text = "";
            string salt = SaltForEncryption;
            int format = (int)MembershipPasswordFormat.Hashed;

            string? encoded = _sut.Encode(text, format, salt);

            Assert.That(encoded, Is.EqualTo(text));
        }
    }
}