using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Abstractions
{
    /// <summary>
    /// Settings for the SQL Membership adapter.
    /// </summary>
    public interface ISqlMembershipSettings
    {
        /// <summary>
        /// The application name.
        /// If you are accessing existing Membership data you should use the name.
        /// </summary>
        string ApplicationName { get; set; }

        /// <summary>
        /// The command timeout in seconds for any database calls.
        /// </summary>
        int CommandTimeoutMilliseconds { get; set; }

        /// <summary>
        /// The SQL database connection string.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// The hash algorithm to use for Membership passwords.
        /// </summary>
        HashAlgorithmType HashAlgorithm { get; set; }

        /// <summary>
        /// Maximum invalid password attempts for a user.
        /// </summary>
        int MaxInvalidPasswordAttempts { get; set; }

        /// <summary>
        /// Minimum required non-alphanumeric characters for a valid password.
        /// </summary>
        int MinRequiredNonAlphanumericCharacters { get; set; }

        /// <summary>
        /// Minimum required length for a valid password.
        /// </summary>
        int MinRequiredPasswordLength { get; set; }

        /// <summary>
        /// The time window, in minutes, during which consecutive failed attempts to provide a valid password or password answer are tracked.
        /// </summary>
        int PasswordAttemptWindow { get; set; }

        /// <summary>
        /// Format for the Password field in the Membership table. Encrypted is not supported.
        /// </summary>
        MembershipPasswordFormat PasswordFormat { get; set; }

        /// <summary>
        /// The timeout when running the custom Regex for password complexity.
        /// </summary>
        int? PasswordStrengthRegexTimeoutMilliseconds { get; set; }

        /// <summary>
        /// Custom Regex for evaluating password complexity.
        /// </summary>
        string PasswordStrengthRegularExpression { get; set; }

        /// <summary>
        /// Toggles the use of password question and answers, used for resetting passwords.
        /// </summary>
        bool RequiresQuestionAndAnswer { get; set; }

        /// <summary>
        /// Toggles whether a unique email must be provided when creating a user.
        /// </summary>
        bool RequiresUniqueEmail { get; set; }
    }
}