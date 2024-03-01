using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter
{
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
        private string _description = "SQL membership provider";

        private MembershipPasswordFormat _passwordFormat = MembershipPasswordFormat.Hashed;

        public SqlMembershipSettings(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Custom application name, originally determined from <c>HostingEnvironment.ApplicationVirtualPath</c> and <c>Process.GetCurrentProcess</c>.
        /// </summary>
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

        public string ConnectionString => _connectionString;

        public string Description
        {
            get => _description;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
                {
                    throw new ArgumentException(Description);
                }

                _description = value.Trim();
            }
        }

        public bool EnablePasswordReset { get; set; } = true;
        public bool RequiresQuestionAndAnswer { get; set; } = true;
        public bool RequiresUniqueEmail { get; set; } = true;

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

        public int? PasswordStrengthRegexTimeoutMilliseconds { get; set; }

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

        public HashAlgorithmType HashAlgorithm { get; set; } = HashAlgorithmType.SHA1;

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