namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for changing a user password.
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Old password.
        /// </summary>
        public string OldPassword { get; }

        /// <summary>
        /// New password.
        /// </summary>
        public string NewPassword { get; }

        /// <summary>
        /// Initialises a new ChangePasswordrequest with the supplied details.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="oldPassword">Old password.</param>
        /// <param name="newPassword">New password.</param>
        public ChangePasswordRequest(string username, string oldPassword, string newPassword)
        {
            Username = username?.Trim() ?? string.Empty;
            OldPassword = oldPassword?.Trim() ?? string.Empty;
            NewPassword = newPassword?.Trim() ?? string.Empty;
        }
    }
}