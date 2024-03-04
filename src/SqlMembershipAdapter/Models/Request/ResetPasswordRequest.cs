namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for resetting a Membership user password.
    /// </summary>
    public class ResetPasswordRequest
    {
        private string _username = string.Empty;
        private string? _passwordAnswer;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username
        {
            get => _username;
            set => _username = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Password.
        /// </summary>
        public string? PasswordAnswer
        {
            get => _passwordAnswer;
            set => _passwordAnswer = value?.Trim();
        }
    }
}