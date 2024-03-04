namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// Defines the hashing algorithms supported from classic ASP.NET Membership.
    /// </summary>
    public enum HashAlgorithmType
    {
        /// <summary>
        /// SHA1.
        /// </summary>
        SHA1,

        /// <summary>
        /// SHA256.
        /// </summary>
        SHA256,

        /// <summary>
        /// SHA384
        /// </summary>
        SHA384,

        /// <summary>
        /// SHA512.
        /// </summary>
        SHA512,

        /// <summary>
        /// MD5.
        /// </summary>
        MD5
    }
}