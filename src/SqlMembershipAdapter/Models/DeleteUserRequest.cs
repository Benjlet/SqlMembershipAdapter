namespace SqlMembershipAdapter.Models
{
    public class DeleteUserRequest
    {
        private string _username = string.Empty;

        public string Username
        {
            get => _username;
            set => _username = value?.Trim() ?? string.Empty;
        }

        public bool DeleteAllRelatedData { get; set; }
    }
}