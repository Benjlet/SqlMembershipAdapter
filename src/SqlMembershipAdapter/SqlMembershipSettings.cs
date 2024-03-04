using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter
{
    /// <inheritdoc cref="ISqlMembershipSettings"/>
    public class SqlMembershipSettings : ISqlMembershipSettings
    {
        private readonly string _connectionString;

        private int _maxInvalidPasswordAttempts = 5;
        private int _minRequiredPasswordLength = 7;
        private int _passwordAttemptWindow = 10;
        private int _minRequiredNonAlphanumericCharacters = 1;
        private int _commandTimeoutSeconds = 30;

        private string _passwordStrengthRegularExpression = string.Empty;
        private string _applicationName = "SqlMembershipProvider";

        private MembershipPasswordFormat _passwordFormat = MembershipPasswordFormat.Hashed;

        /// <summary>
        /// Initialises new SqlMembershipSettings details with the supplied Membership SQL database connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to the Membership SQL database.</param>
        /// <exception cref="ArgumentNullException"/>
        public SqlMembershipSettings(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc/>
        public string ApplicationName
        {
            get => _applicationName;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
                {
                    throw new ArgumentException(ApplicationName);
                }

                _applicationName = value.Trim();
            }
        }

        /// <inheritdoc/>
        public string ConnectionString => _connectionString;

        /// <inheritdoc/>
        public bool RequiresQuestionAndAnswer { get; set; }

        /// <inheritdoc/>
        public bool RequiresUniqueEmail { get; set; } = true;

        /// <inheritdoc/>
        public int CommandTimeoutSeconds
        {
            get => _commandTimeoutSeconds;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(CommandTimeoutSeconds));
                }

                _commandTimeoutSeconds = value;
            }
        }

        /// <inheritdoc/>
        public int MaxInvalidPasswordAttempts
        {
            get => _maxInvalidPasswordAttempts;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxInvalidPasswordAttempts));
                }

                _maxInvalidPasswordAttempts = value;
            }
        }

        /// <inheritdoc/>
        public int PasswordAttemptWindow
        {
            get => _passwordAttemptWindow;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(PasswordAttemptWindow));
                }

                _passwordAttemptWindow = value;
            }
        }

        /// <inheritdoc/>
        public int MinRequiredPasswordLength
        {
            get => _minRequiredPasswordLength;
            set
            {
                if (value < 1 || value > 128)
                {
                    throw new ArgumentOutOfRangeException(nameof(MinRequiredPasswordLength));
                }

                _minRequiredPasswordLength = value;
            }
        }

        /// <inheritdoc/>
        public int MinRequiredNonAlphanumericCharacters
        {
            get => _minRequiredNonAlphanumericCharacters;
            set
            {
                if (value < 0 || value > 128)
                {
                    throw new ArgumentOutOfRangeException(nameof(MinRequiredNonAlphanumericCharacters));
                }

                if (value > MinRequiredPasswordLength)
                {
                    throw new ArgumentException("Cannot be greater than MinRequiredPasswordLength.", nameof(MinRequiredNonAlphanumericCharacters));
                }

                _minRequiredNonAlphanumericCharacters = value;
            }
        }

        /// <inheritdoc/>
        public int? PasswordStrengthRegexTimeoutMilliseconds { get; set; }

        /// <inheritdoc/>
        public MembershipPasswordFormat PasswordFormat
        {
            get => _passwordFormat;
            set
            {
                if (value == MembershipPasswordFormat.Encrypted)
                {
                    throw new ArgumentException("Encrypted password format not supported.");
                }

                _passwordFormat = value;
            }
        }

        /// <inheritdoc/>
        public HashAlgorithmType HashAlgorithm { get; set; } = HashAlgorithmType.SHA1;

        /// <inheritdoc/>
        public string PasswordStrengthRegularExpression
        {
            get => _passwordStrengthRegularExpression;
            set
            {
                _passwordStrengthRegularExpression = value?.Trim() ?? string.Empty;
            }
        }
    }
}