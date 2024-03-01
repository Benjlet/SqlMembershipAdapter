namespace SqlMembershipAdapter.Models
{
    public class ChangePasswordRequest
    {
        private readonly string _username;
        private readonly string _oldPassword;
        private readonly string _newPassword;

        public string Username => _username;
        public string OldPassword => _oldPassword;
        public string NewPassword => _newPassword;

        public ChangePasswordRequest(string username, string oldPassword, string newPassword)
        {
            _username = username?.Trim() ?? string.Empty;
            _oldPassword = oldPassword?.Trim() ?? string.Empty;
            _newPassword = newPassword?.Trim() ?? string.Empty;
        }
    }
}