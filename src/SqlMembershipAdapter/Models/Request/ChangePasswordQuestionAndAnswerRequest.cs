namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for changing a password question and answer.
    /// </summary>
    public class ChangePasswordQuestionAndAnswerRequest
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
        /// The new password question.
        /// </summary>
        public string? NewPasswordQuestion { get; } = null;

        /// <summary>
        /// The new password answer.
        /// </summary>
        public string? NewPasswordAnswer { get; } = null;

        /// <summary>
        /// Initialises a new ChangePasswordQuestionAndAnswerRequest with the supplied details.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="newPasswordQuestion">New password question.</param>
        /// <param name="newPasswordAnswer">New password answer.</param>
        public ChangePasswordQuestionAndAnswerRequest(string username, string password, string? newPasswordQuestion = null, string? newPasswordAnswer = null)
        {
            Username = username?.Trim() ?? string.Empty;
            Password = password?.Trim() ?? string.Empty;

            NewPasswordQuestion = newPasswordQuestion?.Trim() ?? null;
            NewPasswordAnswer = newPasswordAnswer?.Trim() ?? null;
        }
    }
}