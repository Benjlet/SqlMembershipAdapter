namespace SqlMembershipAdapter.Models.Request
{
    /// <summary>
    /// Request details for finding a user.
    /// </summary>
    public class FindUsersRequest
    {
        /// <summary>
        /// Search criteria for finding users.
        /// </summary>
        public string Criteria { get; }

        /// <summary>
        /// Index of the page results from the search.
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// Size of the page results from the search.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Initialises a new find user request.
        /// </summary>
        /// <param name="criteria">Search criteria.</param>
        /// <param name="pageIndex">Index of the page results from the search.</param>
        /// <param name="pageSize">Size of the page results.</param>
        public FindUsersRequest(
            string criteria,
            int pageIndex,
            int pageSize)
        {
            Criteria = criteria?.Trim() ?? string.Empty;
            PageIndex = pageIndex < 0 ? 0 : pageIndex;
            PageSize = pageSize < 1 ? 1 : pageSize;
        }
    }
}