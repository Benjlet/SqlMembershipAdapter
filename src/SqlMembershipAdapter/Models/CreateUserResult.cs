namespace SqlMembershipAdapter.Models
{
    public class CreateUserResult
    {
        private readonly MembershipUser? _user;
        private readonly MembershipCreateStatus _status;

        public MembershipUser? User => _user;
        public MembershipCreateStatus Status => _status;

        public CreateUserResult(MembershipUser? user, MembershipCreateStatus status)
        {
            _user = user;
            _status = status;
        }

        public CreateUserResult(MembershipCreateStatus status)
        {
            _status = status;
        }
    }
}