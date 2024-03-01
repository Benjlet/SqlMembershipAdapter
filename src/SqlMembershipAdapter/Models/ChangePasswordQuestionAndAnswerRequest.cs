namespace SqlMembershipAdapter.Models
{
    public class ChangePasswordQuestionAndAnswerRequest
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string? _newPasswordQuestion = null;
        private readonly string? _newPasswordAnswer = null;

        public string Username => _username;
        public string Password => _password;
        public string? NewPasswordQuestion => _newPasswordQuestion;
        public string? NewPasswordAnswer => _newPasswordAnswer;

        public ChangePasswordQuestionAndAnswerRequest(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public ChangePasswordQuestionAndAnswerRequest(string username, string password, string? newPasswordQuestion = null, string? newPasswordAnswer = null)
        {
            _username = username?.Trim() ?? string.Empty;
            _password = password?.Trim() ?? string.Empty;

            _newPasswordQuestion = newPasswordQuestion?.Trim() ?? null;
            _newPasswordAnswer = newPasswordAnswer?.Trim() ?? null;
        }
    }
}