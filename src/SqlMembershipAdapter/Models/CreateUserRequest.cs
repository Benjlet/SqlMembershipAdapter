namespace SqlMembershipAdapter.Models
{
    public class CreateUserRequest
    {
        private readonly string? _username;
        private readonly string? _password;
        private readonly string? _email;
        private readonly string? _passwordQuestion;
        private readonly string? _passwordAnswer;
        private readonly Guid? _providerUserKey;
        private readonly bool _isApproved;

        public string? Username => _username;
        public string? Password => _password;
        public string? Email => _email;
        public string? PasswordQuestion => _passwordQuestion;
        public string? PasswordAnswer => _passwordAnswer;
        public bool IsApproved => _isApproved;
        public Guid? ProviderUserKey => _providerUserKey;

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

        public CreateUserRequest(
            string username,
            string password)
        {
            _username = username?.Trim() ?? string.Empty;
            _password = password?.Trim() ?? string.Empty;
        }
    }
}