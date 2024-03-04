namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for changing a user password.
    /// </summary>
    public class ChangePasswordRequest
    {
        private readonly string _username;
        private readonly string _oldPassword;
        private readonly string _newPassword;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username => _username;

        /// <summary>
        /// Old password.
        /// </summary>
        public string OldPassword => _oldPassword;

        /// <summary>
        /// New password.
        /// </summary>
        public string NewPassword => _newPassword;

        /// <summary>
        /// Initialises a new ChangePasswordrequest with the supplied details.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="oldPassword">Old password.</param>
        /// <param name="newPassword">New password.</param>
        public ChangePasswordRequest(string username, string oldPassword, string newPassword)
        {
            _username = username?.Trim() ?? string.Empty;
            _oldPassword = oldPassword?.Trim() ?? string.Empty;
            _newPassword = newPassword?.Trim() ?? string.Empty;
        }
    }
}