using System.Text.RegularExpressions;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;

namespace SqlMembershipAdapter
{
    internal class SqlMembershipValidator : ISqlMembershipValidator
    {
        private readonly ISqlMembershipSettings _settings;

        public SqlMembershipValidator(
            ISqlMembershipSettings settings)
        {
            _settings = settings;
        }

        public bool ValidatePassword(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length <= 128 && password.Length >= _settings.MinRequiredPasswordLength;
        }

        public bool ValidatePageRange(int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
            {
                return false;
            }

            if (pageSize < 1)
            {
                return false;
            }

            long upperBound = (long)pageIndex * pageSize + pageSize - 1;

            return upperBound <= int.MaxValue;
        }

        public bool ValidatePasswordComplexity(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            int alphanumericCharCount = password.Count(c => !char.IsLetterOrDigit(c));

            if (alphanumericCharCount < _settings.MinRequiredNonAlphanumericCharacters)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_settings.PasswordStrengthRegularExpression))
            {
                return _settings.PasswordStrengthRegexTimeoutMilliseconds.HasValue
                    ? Regex.IsMatch(password, _settings.PasswordStrengthRegularExpression, RegexOptions.None, TimeSpan.FromMilliseconds(_settings.PasswordStrengthRegexTimeoutMilliseconds.Value))
                    : Regex.IsMatch(password, _settings.PasswordStrengthRegularExpression, RegexOptions.None);
            }

            return true;
        }

        public bool ValidatePasswordQuestion(string? passwordQuestion)
        {
            return (!string.IsNullOrWhiteSpace(passwordQuestion) || !_settings.RequiresQuestionAndAnswer) && (passwordQuestion == null || passwordQuestion.Length <= 256);
        }

        public bool ValidatePasswordAnswer(string? passwordAnswer)
        {
            return (!string.IsNullOrWhiteSpace(passwordAnswer) || !_settings.RequiresQuestionAndAnswer) && (passwordAnswer == null || passwordAnswer.Length <= 128);
        }

        public bool ValidateUsername(string? username)
        {
            return !string.IsNullOrWhiteSpace(username) && username.Length <= 256 && !username.Contains(",");
        }

        public bool ValidateEmail(string? email)
        {
            return (!string.IsNullOrWhiteSpace(email) || !_settings.RequiresUniqueEmail) && (email == null || email.Length <= 256);
        }

        public bool ValidateChangePasswordQuestionAnswer(ChangePasswordQuestionAndAnswerRequest request, out string? invalidParam)
        {
            invalidParam = null;

            if (!ValidateUsername(request.Username))
            {
                invalidParam = nameof(request.Username);
                return false;
            }

            if (!ValidatePassword(request.Password))
            {
                invalidParam = nameof(request.Password);
                return false;
            }

            if (!ValidatePasswordQuestion(request.NewPasswordQuestion))
            {
                invalidParam = nameof(request.NewPasswordQuestion);
                return false;
            }

            if (!ValidatePasswordAnswer(request.NewPasswordAnswer))
            {
                invalidParam = nameof(request.NewPasswordAnswer);
                return false;
            }

            return true;
        }

        public bool ValidateChangePasswordRequest(ChangePasswordRequest request, out string? invalidParam)
        {
            invalidParam = null;

            if (!ValidateUsername(request.Username))
            {
                invalidParam = nameof(request.Username);
                return false;
            }

            if (!ValidatePassword(request.NewPassword))
            {
                invalidParam = nameof(request.NewPassword);
                return false;
            }

            if (!ValidatePassword(request.OldPassword))
            {
                invalidParam = nameof(request.OldPassword);
                return false;
            }

            return true;
        }

        public bool ValidateCreateUserRequest(CreateUserRequest request, out MembershipCreateStatus status)
        {
            status = MembershipCreateStatus.Success;

            if (!ValidatePassword(request.Password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return false;
            }

            if (!ValidatePasswordAnswer(request.PasswordAnswer))
            {
                status = MembershipCreateStatus.InvalidAnswer;
                return false;
            }

            if (!ValidateUsername(request.Username))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return false;
            }

            if (!ValidateEmail(request.Email))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return false;
            }

            if (!ValidatePasswordQuestion(request.PasswordQuestion))
            {
                status = MembershipCreateStatus.InvalidQuestion;
                return false;
            }

            if (!ValidatePasswordComplexity(request.Password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return false;
            }

            return true;
        }
    }
}