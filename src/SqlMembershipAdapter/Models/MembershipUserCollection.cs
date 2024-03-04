namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// A collection of Membership user details when finding users..
    /// </summary>
    public class MembershipUserCollection
    {
        private readonly List<MembershipUser> _users;
        private readonly int _count;

        /// <summary>
        /// A list of Membership users from the search.
        /// </summary>
        public List<MembershipUser> Users => _users;

        /// <summary>
        /// The count of Membership users found in the search.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Initialises a new MembershipUserCollection with the supplied list of Membership users and the count of those retrieved.
        /// </summary>
        /// <param name="users">The retrieved users.</param>
        /// <param name="count">The number of users in the results.</param>
        public MembershipUserCollection(List<MembershipUser> users, int count)
        {
            _users = users;
            _count = count;
        }
    }
}