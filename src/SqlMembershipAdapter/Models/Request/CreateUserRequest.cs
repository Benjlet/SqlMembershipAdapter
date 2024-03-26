namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for creating a new Membership user.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Username; this is often the same as the email.
        /// </summary>
        public string? Username { get; }

        /// <summary>
        /// Password.
        /// </summary>
        public string? Password { get; }

        /// <summary>
        /// Email.
        /// </summary>
        public string? Email { get; }

        /// <summary>
        /// Password question.
        /// </summary>
        public string? PasswordQuestion { get; }

        /// <summary>
        /// Password answer.
        /// </summary>
        public string? PasswordAnswer { get; }

        /// <summary>
        /// Approval status of the user.
        /// </summary>
        public bool IsApproved { get; }

        /// <summary>
        /// The provider user key of the user (UserId).
        /// </summary>
        public Guid? ProviderUserKey { get; }

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
            bool isApproved,
            Guid? providerUserKey)
        {
            Username = username?.Trim();
            Password = password?.Trim();
            Email = email?.Trim();
            PasswordQuestion = passwordQuestion?.Trim();
            PasswordAnswer = passwordAnswer?.Trim();
            ProviderUserKey = providerUserKey;
            IsApproved = isApproved;
        }

        /// <summary>
        /// Initialises a new CreateUserRequest with the supplied details.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="email">Email</param>
        /// <param name="passwordQuestion">Password question.</param>
        /// <param name="passwordAnswer">Password answer.</param>
        /// <param name="providerUserKey">User ID. Set to null for new users. Will attempt to parse as a Guid.</param>
        /// <param name="isApproved">Approval status of the user.</param>
        public CreateUserRequest(
            string username,
            string password,
            string? email,
            string? passwordQuestion,
            string? passwordAnswer,
            bool isApproved,
            object providerUserKey)
        {
            Username = username?.Trim();
            Password = password?.Trim();
            Email = email?.Trim();
            PasswordQuestion = passwordQuestion?.Trim();
            PasswordAnswer = passwordAnswer?.Trim();
            ProviderUserKey = providerUserKey == null || !Guid.TryParse(providerUserKey.ToString(), out Guid userKey) ? null : userKey;
            IsApproved = isApproved;
        }
    }
}