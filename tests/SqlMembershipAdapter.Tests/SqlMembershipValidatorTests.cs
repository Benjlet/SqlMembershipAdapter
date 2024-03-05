using Moq;
using NUnit.Framework;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;

namespace SqlMembershipAdapter.Tests
{
    public class SqlMembershipValidatorTests
    {
        private Mock<ISqlMembershipSettings> _mockSettings;
        private SqlMembershipValidator _sut;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISqlMembershipSettings>();
            _sut = new SqlMembershipValidator(_mockSettings.Object);
        }

        [Test]
        public void ValidateUsername_Comma_ReturnsFalse()
        {
            bool isValid = _sut.ValidateUsername("Username,");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_TooLong_ReturnsFalse()
        {
            bool isValid = _sut.ValidateUsername(new string('A', 400));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Null_ReturnsFalse()
        {
            bool isValid = _sut.ValidateUsername(null);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Empty_ReturnsFalse()
        {
            bool isValid = _sut.ValidateUsername(string.Empty);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Whitespace_ReturnsFalse()
        {
            bool isValid = _sut.ValidateUsername("    ");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateUsername_Valid_ReturnsTrue()
        {
            bool isValid = _sut.ValidateUsername("Username");
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePassword_Empty_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePassword(string.Empty);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePassword_Whitespace_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePassword("    ");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePassword_Null_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePassword(null);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePassword_TooLong_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePassword(new string('A', 200));
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePassword_LessThanRequiredLength_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.MinRequiredPasswordLength).Returns(12);

            bool isValid = _sut.ValidatePassword("hunter2");

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePassword_Valid_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePassword("hunter2");
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePageRange_IndexLessThanZero_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePageRange(-1, 3);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePageRange_SizeLessThanOne_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePageRange(3, 0);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePageRange_UpperBoundGreatThanMaxInt_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePageRange(int.MaxValue, int.MaxValue);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePageRange_IsValid_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePageRange(3, 5);
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordComplexity_Null_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePasswordComplexity(null);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordComplexity_Empty_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePasswordComplexity(string.Empty);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordComplexity_Whitespace_ReturnsFalse()
        {
            bool isValid = _sut.ValidatePasswordComplexity("    ");
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordComplexity_NoRegularExpression_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePasswordComplexity("hunter2");
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordComplexity_FailsExpression_ReturnsFalse()
        {
            string eightCharactersOneLetterAndNumberRegex = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$";
            
            _mockSettings.SetupGet(x => x.PasswordStrengthRegularExpression).Returns(eightCharactersOneLetterAndNumberRegex);

            bool isValid = _sut.ValidatePasswordComplexity("hunter2");

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordComplexity_PassesExpression_TimeoutRetrieved_ReturnsTrue()
        {
            string threeCharactersOneLetterAndNumberRegex = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{3,}$";

            _mockSettings.SetupGet(x => x.PasswordStrengthRegularExpression).Returns(threeCharactersOneLetterAndNumberRegex);
            _mockSettings.SetupGet(x => x.PasswordStrengthRegexTimeoutMilliseconds).Returns(5000);

            bool isValid = _sut.ValidatePasswordComplexity("hunter2");

            _mockSettings.Verify(x => x.PasswordStrengthRegexTimeoutMilliseconds, Times.Exactly(2));

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordQuestion_Null_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordQuestion(null);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordQuestion_Empty_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordQuestion(string.Empty);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordQuestion_Whitespace_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordQuestion("     ");

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordQuestion_TooLong_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordQuestion(new string('A', 400));

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordQuestion_Valid_Required_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePasswordQuestion("What is the airspeed velocity of an unladen swallow?");
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordAnswer_Null_NotRequired_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePasswordAnswer(null);
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordAnswer_Empty_NotRequired_ReturnsTrue()
        {
            bool isValid = _sut.ValidatePasswordAnswer(string.Empty);
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidatePasswordAnswer_Null_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordAnswer(null);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordAnswer_TooLong_Required_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordAnswer(new string('A', 400));

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidatePasswordAnswer_Valid_Required_ReturnsTrue()
        {
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidatePasswordAnswer("an African or European swallow?");

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateEmail_Required_Null_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);

            bool isValid = _sut.ValidateEmail(null);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateEmail_NotRequired_Null_ReturnsTrue()
        {
            bool isValid = _sut.ValidateEmail(null);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateEmail_Required_Valid_ReturnsTrue()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);

            bool isValid = _sut.ValidateEmail("example@email.com");

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateEmail_Required_Empty_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);

            bool isValid = _sut.ValidateEmail(string.Empty);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateEmail_NotRequired_Empty_ReturnsTrue()
        {
            bool isValid = _sut.ValidateEmail(string.Empty);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateEmail_Required_TooLong_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);

            bool isValid = _sut.ValidateEmail(new string('A', 400));

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateChangePasswordQuestionAnswer_InvalidUsername_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateChangePasswordQuestionAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username: "",
                password: "hunter2",
                newPasswordQuestion: "What is the airspeed velocity of an unladen swallow?",
                newPasswordAnswer: "an African or European swallow?"), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("Username"));
            });
        }

        [Test]
        public void ValidateChangePasswordQuestionAnswer_InvalidPassword_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateChangePasswordQuestionAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username: "username",
                password: "",
                newPasswordQuestion: "What is the airspeed velocity of an unladen swallow?",
                newPasswordAnswer: "an African or European swallow?"), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("Password"));
            });
        }

        [Test]
        public void ValidateChangePasswordQuestionAnswer_InvalidPasswordQuestion_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateChangePasswordQuestionAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username: "username",
                password: "hunter2",
                newPasswordQuestion: "",
                newPasswordAnswer: "an African or European swallow?"), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("NewPasswordQuestion"));
            });
        }

        [Test]
        public void ValidateChangePasswordQuestionAnswer_InvalidPasswordAnswer_ReturnsFalse()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateChangePasswordQuestionAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username: "username",
                password: "hunter2",
                newPasswordQuestion: "What is the airspeed velocity of an unladen swallow?",
                newPasswordAnswer: null), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("NewPasswordAnswer"));
            });
        }

        [Test]
        public void ValidateChangePasswordQuestionAnswer_Valid_ReturnsTrue()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateChangePasswordQuestionAnswer(new ChangePasswordQuestionAndAnswerRequest(
                username: "username",
                password: "hunter2",
                newPasswordQuestion: "What is the airspeed velocity of an unladen swallow?",
                newPasswordAnswer: "an African or European swallow?"), out string? invalidParam);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateChangePasswordRequest_InvalidUsername_ReturnsFalse()
        {
            bool isValid = _sut.ValidateChangePasswordRequest(new ChangePasswordRequest(
                username: "",
                oldPassword: "hunter2",
                newPassword: "hunter3"), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("Username"));
            });
        }

        [Test]
        public void ValidateChangePasswordRequest_InvalidNewPassword_ReturnsFalse()
        {
            bool isValid = _sut.ValidateChangePasswordRequest(new ChangePasswordRequest(
                username: "username",
                oldPassword: "hunter2",
                newPassword: ""), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("NewPassword"));
            });
        }

        [Test]
        public void ValidateChangePasswordRequest_InvalidOldPassword_ReturnsFalse()
        {
            bool isValid = _sut.ValidateChangePasswordRequest(new ChangePasswordRequest(
                username: "username",
                oldPassword: "",
                newPassword: "hunter3"), out string? invalidParam);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(invalidParam, Is.EqualTo("OldPassword"));
            });
        }

        [Test]
        public void ValidateChangePasswordRequest_Valid_ReturnsTrue()
        {
            bool isValid = _sut.ValidateChangePasswordRequest(new ChangePasswordRequest(
                username: "username",
                oldPassword: "hunter2",
                newPassword: "hunter3"), out string? invalidParam);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateCreateUserRequest_Valid_ReturnsTrue_Created()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "hunter2",
                email: "email@example.com",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.True);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.Success));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_InvalidPassword_ReturnsFalse_InvalidPassword()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "",
                email: "email@example.com",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidPassword));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_InvalidPasswordAnswer_ReturnsFalse_InvalidAnswer()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "hunter2",
                email: "email@example.com",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidAnswer));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_InvalidUsername_ReturnsFalse_InvalidUsername()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "",
                password: "hunter2",
                email: "email@example.com",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidUserName));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_InvalidEmail_ReturnsFalse_InvalidEmail()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "hunter2",
                email: "",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidEmail));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_InvalidQuestion_ReturnsFalse_InvalidQuestion()
        {
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "hunter2",
                email: "",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidEmail));
            });
        }

        [Test]
        public void ValidateCreateUserRequest_WeakPassword_ReturnsFalse_InvalidPassword()
        {
            string nineCharactersOneLetterAndNumberRegex = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{9,}$";

            _mockSettings.SetupGet(x => x.PasswordStrengthRegularExpression).Returns(nineCharactersOneLetterAndNumberRegex);
            _mockSettings.SetupGet(x => x.RequiresUniqueEmail).Returns(true);
            _mockSettings.SetupGet(x => x.RequiresQuestionAndAnswer).Returns(true);

            bool isValid = _sut.ValidateCreateUserRequest(new CreateUserRequest(
                username: "username",
                password: "hunter2",
                email: "email@example.com",
                passwordQuestion: "What is the airspeed velocity of an unladen swallow?",
                passwordAnswer: "an African or European swallow?",
                providerUserKey: null,
                isApproved: true), out MembershipCreateStatus status);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.False);
                Assert.That(status, Is.EqualTo(MembershipCreateStatus.InvalidPassword));
            });
        }
    }
}