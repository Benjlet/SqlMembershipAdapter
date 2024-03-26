namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// User credentials to validate.
    /// </summary>
    public class ValidateUserRequest
    {
        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Initialises a new request to validate a user.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public ValidateUserRequest(
            string username,
            string password)
        {
            Username = username?.Trim() ?? string.Empty;
            Password = password?.Trim() ?? string.Empty;
        }
    }
}