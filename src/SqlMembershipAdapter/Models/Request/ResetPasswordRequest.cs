namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for resetting a Membership user password.
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Password.
        /// </summary>
        public string? PasswordAnswer { get; }

        /// <summary>
        /// Initialises a new password reset request.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="passwordAnswer">Password.</param>
        public ResetPasswordRequest(
            string username,
            string passwordAnswer)
        {
            Username = username?.Trim() ?? string.Empty;
            PasswordAnswer = passwordAnswer?.Trim();
        }
    }
}