namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for changing a password question and answer.
    /// </summary>
    public class ChangePasswordQuestionAndAnswerRequest
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string? _newPasswordQuestion = null;
        private readonly string? _newPasswordAnswer = null;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username => _username;

        /// <summary>
        /// Password.
        /// </summary>
        public string Password => _password;

        /// <summary>
        /// The new password question.
        /// </summary>
        public string? NewPasswordQuestion => _newPasswordQuestion;

        /// <summary>
        /// The new password answer.
        /// </summary>
        public string? NewPasswordAnswer => _newPasswordAnswer;

        /// <summary>
        /// Initialises a new ChangePasswordQuestionAndAnswerRequest with the supplied details.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="newPasswordQuestion">New password question.</param>
        /// <param name="newPasswordAnswer">New password answer.</param>
        public ChangePasswordQuestionAndAnswerRequest(string username, string password, string? newPasswordQuestion = null, string? newPasswordAnswer = null)
        {
            _username = username?.Trim() ?? string.Empty;
            _password = password?.Trim() ?? string.Empty;

            _newPasswordQuestion = newPasswordQuestion?.Trim() ?? null;
            _newPasswordAnswer = newPasswordAnswer?.Trim() ?? null;
        }
    }
}