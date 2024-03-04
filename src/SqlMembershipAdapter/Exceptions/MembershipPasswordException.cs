namespace SqlMembershipAdapter.Exceptions
{
    /// <summary>
    /// Exception raised for a bad password during change or reset password events.
    /// </summary>
    public class MembershipPasswordException : Exception
    {
        /// <summary>
        /// Initialises a new Membership Password Exception.
        /// </summary>
        public MembershipPasswordException()
        {
        }

        /// <summary>
        /// Initialises a new Membership Password Exception with the supplied message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MembershipPasswordException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initialises a new Membership Password Exception with the supplied message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MembershipPasswordException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}