namespace SqlMembershipAdapter.Exceptions
{
    /// <summary>
    /// Known exception for SQL Membership database errors.
    /// </summary>
    public class ProviderException : Exception
    {
        /// <summary>
        /// Initialises a new ProviderException.
        /// </summary>
        public ProviderException()
        {
        }

        /// <summary>
        /// Initialises a new ProviderException with the supplied exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ProviderException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initialises a new ProviderException with the supplied message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception details.</param>
        public ProviderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}