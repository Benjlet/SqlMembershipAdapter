namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for deleting a Membership user.
    /// </summary>
    public class DeleteUserRequest
    {
        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Flag for deleting all related user data.
        /// </summary>
        public bool DeleteAllRelatedData { get; }

        /// <summary>
        /// Initialises a new request to delete a Membership user.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="deleteAllRelatedData">Flag for deleting all related user data.</param>
        public DeleteUserRequest(
            string username,
            bool deleteAllRelatedData)
        {
            Username = username?.Trim() ?? string.Empty;
            DeleteAllRelatedData = deleteAllRelatedData;
        }
    }
}