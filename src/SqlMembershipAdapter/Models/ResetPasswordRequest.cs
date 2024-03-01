namespace SqlMembershipAdapter.Models
{
    public class ResetPasswordRequest
    {
        private string _username = string.Empty;
        private string? _passwordAnswer;

        public string Username
        {
            get => _username;
            set => _username = value?.Trim() ?? string.Empty;
        }

        public string? PasswordAnswer
        {
            get => _passwordAnswer;
            set => _passwordAnswer = value?.Trim();
        }
    }
}