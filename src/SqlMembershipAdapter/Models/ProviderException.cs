namespace SqlMembershipAdapter.Models
{
    public class ProviderException : Exception
    {
        public ProviderException()
        {
        }

        public ProviderException(string? message) : base(message)
        {
        }

        public ProviderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}