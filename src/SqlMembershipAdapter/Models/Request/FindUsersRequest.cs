namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for finding a user.
    /// </summary>
    public class FindUsersRequest
    {
        private string _criteria = string.Empty;
        private int pageIndex;
        private int pageSize;

        /// <summary>
        /// Search criteria for finding users.
        /// </summary>
        public string Criteria
        {
            get => _criteria;
            set => _criteria = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Index of the page results from the search.
        /// </summary>
        public int PageIndex
        {
            get => pageIndex;
            set => pageIndex = value < 0 ? 0 : value;
        }

        /// <summary>
        /// Size of the page results from the search.
        /// </summary>
        public int PageSize
        {
            get => pageSize;
            set => pageSize = value < 1 ? 1 : value;
        }
    }
}