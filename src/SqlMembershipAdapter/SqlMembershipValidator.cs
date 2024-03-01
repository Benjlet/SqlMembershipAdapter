using System.Text.RegularExpressions;
using SqlMembershipAdapter.Abstractions;

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
            return (passwordQuestion != null || !_settings.RequiresQuestionAndAnswer) && (passwordQuestion == null || (passwordQuestion.Length != 0 && passwordQuestion.Length <= 256));
        }

        public bool ValidatePasswordAnswer(string? passwordAnswer)
        {
            return (passwordAnswer != null || !_settings.RequiresQuestionAndAnswer) && (passwordAnswer == null || passwordAnswer.Length <= 128);
        }

        public bool ValidateUsername(string? username)
        {
            return !string.IsNullOrWhiteSpace(username) && username.Length <= 256 && !username.Contains(",");
        }

        public bool ValidateEmail(string? email)
        {
            return (!string.IsNullOrWhiteSpace(email) || !_settings.RequiresUniqueEmail) && (email == null || email.Length <= 256);
        }
    }
}