namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// Format for the Password field in the Membership table.
    /// Encrypted is not supported.
    /// </summary>
    public enum MembershipPasswordFormat
    {
        /// <summary>
        /// The password is stored in cleartext in the database.
        /// </summary>
        Clear = 0,

        /// <summary>
        /// The password is cryptographically hashed and stored in the database.
        /// </summary>
        Hashed = 1,

        /// <summary>
        /// The password is encrypted using reversible encryption (machineKey) and stored in the database.
        /// </summary>
        Encrypted = 2
    }
}