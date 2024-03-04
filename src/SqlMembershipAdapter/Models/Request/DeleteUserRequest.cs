namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for deleting a Membership user.
    /// </summary>
    public class DeleteUserRequest
    {
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
        /// Flag for deleting all related user data.
        /// </summary>
        public bool DeleteAllRelatedData { get; set; }
    }
}