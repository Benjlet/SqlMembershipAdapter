namespace SqlMembershipAdapter.Models
{
    public class MembershipPasswordException : Exception
    {
        public MembershipPasswordException()
        {
        }

        public MembershipPasswordException(string? message) : base(message)
        {
        }

        public MembershipPasswordException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}