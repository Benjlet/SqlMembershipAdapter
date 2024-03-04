namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// User credentials to validate.
    /// </summary>
    public class ValidateUserRequest
    {
        private string _password = string.Empty;
        private string _username = string.Empty;

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
        public string Password
        {
            get => _password;
            set => _password = value?.Trim() ?? string.Empty;
        }
    }
}