using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Abstractions
{
    public interface ISqlMembershipSettings
    {
        string ApplicationName { get; set; }
        int CommandTimeoutSeconds { get; set; }
        string ConnectionString { get; }
        string Description { get; set; }
        bool EnablePasswordReset { get; set; }
        HashAlgorithmType HashAlgorithm { get; set; }
        int MaxInvalidPasswordAttempts { get; set; }
        int MinRequiredNonAlphanumericCharacters { get; set; }
        int MinRequiredPasswordLength { get; set; }
        int PasswordAttemptWindow { get; set; }
        MembershipPasswordFormat PasswordFormat { get; set; }
        int? PasswordStrengthRegexTimeoutMilliseconds { get; set; }
        string PasswordStrengthRegularExpression { get; set; }
        bool RequiresQuestionAndAnswer { get; set; }
        bool RequiresUniqueEmail { get; set; }
    }
}