namespace SqlMembershipAdapter.Models
{
    public class ValidateUserRequest
    {
        private string _password = string.Empty;
        private string _username = string.Empty;

        public string Username
        {
            get => _username;
            set => _username = value?.Trim() ?? string.Empty;
        }

        public string Password
        {
            get => _password;
            set => _password = value?.Trim() ?? string.Empty;
        }
    }
}