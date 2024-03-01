namespace SqlMembershipAdapter.Models
{
    public class MembershipUserCollection
    {
        private readonly List<MembershipUser> _users;
        private readonly int _count;

        public List<MembershipUser> Users => _users;
        public int Count => _count;

        public MembershipUserCollection(List<MembershipUser> users, int count)
        {
            _users = users;
            _count = count;
        }
    }
}