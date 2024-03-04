namespace SqlMembershipAdapter.Models.Result
{
    /// <summary>
    /// Result of creating a new Membership user.
    /// </summary>
    public class CreateUserResult
    {
        private readonly MembershipUser? _user;
        private readonly MembershipCreateStatus _status;

        /// <summary>
        /// The created Membership user.
        /// </summary>
        public MembershipUser? User => _user;

        /// <summary>
        /// The status of the Membership user creation request.
        /// </summary>
        public MembershipCreateStatus Status => _status;

        /// <summary>
        /// Initialises a new CreateUserResult with the supplied user details and creation status.
        /// </summary>
        /// <param name="user">The membership user.</param>
        /// <param name="status">The status of the creation request.</param>
        public CreateUserResult(MembershipUser? user, MembershipCreateStatus status)
        {
            _user = user;
            _status = status;
        }

        /// <summary>
        /// Initialises a new CreateUserResult with the supplied user details and creation status.
        /// </summary>
        /// <param name="status">The status of the creation request.</param>
        public CreateUserResult(MembershipCreateStatus status)
        {
            _status = status;
        }
    }
}