namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for creating a new Membership user.
    /// </summary>
    public class CreateUserRequest
    {
        private readonly string? _username;
        private readonly string? _password;
        private readonly string? _email;
        private readonly string? _passwordQuestion;
        private readonly string? _passwordAnswer;
        private readonly Guid? _providerUserKey;
        private readonly bool _isApproved;

        /// <summary>
        /// Username; this is often the same as the email.
        /// </summary>
        public string? Username => _username;

        /// <summary>
        /// Password.
        /// </summary>
        public string? Password => _password;

        /// <summary>
        /// Email.
        /// </summary>
        public string? Email => _email;

        /// <summary>
        /// Password question.
        /// </summary>
        public string? PasswordQuestion => _passwordQuestion;

        /// <summary>
        /// Password answer.
        /// </summary>
        public string? PasswordAnswer => _passwordAnswer;

        /// <summary>
        /// Approval status of the user.
        /// </summary>
        public bool IsApproved => _isApproved;

        /// <summary>
        /// The provider user key of the user (UserId).
        /// </summary>
        public Guid? ProviderUserKey => _providerUserKey;

        /// <summary>
        /// Initialises a new CreateUserRequest with the supplied details.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="email">Email</param>
        /// <param name="passwordQuestion">Password question.</param>
        /// <param name="passwordAnswer">Password answer.</param>
        /// <param name="providerUserKey">User ID. Set to null for new users.</param>
        /// <param name="isApproved">Approval status of the user.</param>
        public CreateUserRequest(
            string username,
            string password,
            string? email,
            string? passwordQuestion,
            string? passwordAnswer,
            Guid? providerUserKey,
            bool isApproved)
        {
            _username = username?.Trim();
            _password = password?.Trim();
            _email = email?.Trim();
            _passwordQuestion = passwordQuestion?.Trim();
            _passwordAnswer = passwordAnswer?.Trim();
            _providerUserKey = providerUserKey;
            _isApproved = isApproved;
        }
    }
}