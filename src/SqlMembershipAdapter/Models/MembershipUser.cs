namespace SqlMembershipAdapter.Models
{
    [Serializable]
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

        public Guid? ProviderUserKey => _providerUserKey;

        public string ProviderName => _providerName;
        public string? PasswordQuestion => _passwordQuestion;
        public string? UserName => _userName;
        public string? Comment => _comment;
        public string? Email => _email;

        public bool IsApproved => _isApproved;
        public bool IsLockedOut => _isLockedOut;

        public DateTime CreationDate => _creationDate.ToLocalTime();
        public DateTime LastLoginDate => _lastLoginDate.ToLocalTime();
        public DateTime LastLockoutDate => _lastLockoutDate.ToLocalTime();
        public DateTime LastActivityDate => _lastActivityDate.ToLocalTime();
        public DateTime LastPasswordChangedDate => _lastPasswordChangedDate.ToLocalTime();

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

        public virtual bool IsOnline(int timeWindow = 15)
        {
            TimeSpan timeSpan = new(0, timeWindow, 0);
            DateTime dateTime = DateTime.UtcNow.Subtract(timeSpan);

            return LastActivityDate.ToUniversalTime() > dateTime;
        }

        public override string ToString()
        {
            return UserName ?? string.Empty;
        }
    }
}
