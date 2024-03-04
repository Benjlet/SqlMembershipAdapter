namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// User details of a user stored in the Membership tables.
    /// </summary>
    public class MembershipUser
    {
        private readonly Guid? _providerUserKey;

        private readonly string _providerName;
        private readonly string? _passwordQuestion;
        private readonly string? _userName;
        private readonly string? _comment;
        private readonly string? _email;

        private readonly bool _isApproved;
        private readonly bool _isLockedOut;

        private readonly DateTime _creationDate;
        private readonly DateTime _lastLoginDate;
        private readonly DateTime _lastLockoutDate;
        private readonly DateTime _lastActivityDate;
        private readonly DateTime _lastPasswordChangedDate;

        /// <summary>
        /// Provider User Key (the 'UserId' column).
        /// </summary>
        public Guid? ProviderUserKey => _providerUserKey;

        /// <summary>
        /// Provider name.
        /// </summary>
        public string ProviderName => _providerName;

        /// <summary>
        /// Password question.
        /// </summary>
        public string? PasswordQuestion => _passwordQuestion;

        /// <summary>
        /// Username.
        /// </summary>
        public string? UserName => _userName;

        /// <summary>
        /// Comment.
        /// </summary>
        public string? Comment => _comment;

        /// <summary>
        /// Email.
        /// </summary>
        public string? Email => _email;

        /// <summary>
        /// User account approval state.
        /// </summary>
        public bool IsApproved => _isApproved;

        /// <summary>
        /// User account lock state.
        /// </summary>
        public bool IsLockedOut => _isLockedOut;

        /// <summary>
        /// Creation date of the Membership user record.
        /// </summary>
        public DateTime CreationDate => _creationDate.ToLocalTime();

        /// <summary>
        /// Last login date.
        /// </summary>
        public DateTime LastLoginDate => _lastLoginDate.ToLocalTime();

        /// <summary>
        /// Last account lockout date.
        /// </summary>
        public DateTime LastLockoutDate => _lastLockoutDate.ToLocalTime();

        /// <summary>
        /// Last activity date.
        /// </summary>
        public DateTime LastActivityDate => _lastActivityDate.ToLocalTime();

        /// <summary>
        /// Last password changed date.
        /// </summary>
        public DateTime LastPasswordChangedDate => _lastPasswordChangedDate.ToLocalTime();

        /// <summary>
        /// Initialises a new Membership user with the supplied details.
        /// </summary>
        /// <param name="providerName">Provider name.</param>
        /// <param name="userName">Username.</param>
        /// <param name="providerUserKey">Provider user key (UserId); you may set to <see langword="null"/> for new users.</param>
        /// <param name="email">Email.</param>
        /// <param name="passwordQuestion">Password question.</param>
        /// <param name="comment">Comment.</param>
        /// <param name="isApproved">User approval state.</param>
        /// <param name="isLockedOut">User lock state.</param>
        /// <param name="creationDate">Creation date.</param>
        /// <param name="lastLoginDate">Last login date.</param>
        /// <param name="lastActivityDate">Last activity date.</param>
        /// <param name="lastPasswordChangedDate">Last password changed date.</param>
        /// <param name="lastLockoutDate">Last lockout date.</param>
        /// <exception cref="ArgumentNullException"/>
        public MembershipUser(
            string providerName,
            string? userName,
            Guid? providerUserKey,
            string? email,
            string? passwordQuestion,
            string? comment,
            bool isApproved,
            bool isLockedOut,
            DateTime creationDate,
            DateTime lastLoginDate,
            DateTime lastActivityDate,
            DateTime lastPasswordChangedDate,
            DateTime lastLockoutDate)
        {
            _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
            _providerUserKey = providerUserKey;

            _email = email?.Trim();
            _userName = userName?.Trim();
            _comment = comment?.Trim();
            _passwordQuestion = passwordQuestion?.Trim();

            _isApproved = isApproved;
            _isLockedOut = isLockedOut;

            _creationDate = creationDate.ToUniversalTime();
            _lastLoginDate = lastLoginDate.ToUniversalTime();
            _lastActivityDate = lastActivityDate.ToUniversalTime();
            _lastPasswordChangedDate = lastPasswordChangedDate.ToUniversalTime();
            _lastLockoutDate = lastLockoutDate.ToUniversalTime();
        }

        /// <summary>
        /// Determines if the user's last activity date is within the supplied time window.
        /// </summary>
        /// <param name="timeWindow">The time window for when the user last had activity.</param>
        /// <returns>Indicator of if the user had activity within the time window.</returns>
        public bool IsOnline(int timeWindow = 15)
        {
            TimeSpan timeSpan = new(0, timeWindow, 0);
            DateTime dateTime = DateTime.UtcNow.Subtract(timeSpan);
            return LastActivityDate.ToUniversalTime() > dateTime;
        }

        /// <summary>
        /// Returns the Username as a string.
        /// </summary>
        /// <returns>Username.</returns>
        public override string ToString()
        {
            return UserName ?? string.Empty;
        }
    }
}
