namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// The raw password record from the Membership table.
    /// </summary>
    public class PasswordData
    {
        /// <summary>
        /// Password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Password salt.
        /// </summary>
        public string? PasswordSalt { get; set; }

        /// <summary>
        /// The password format code: 0 = Clear; 1 = Hashed; 2 = Encrypted.
        /// </summary>
        public int PasswordFormatCode { get; set; }
    }
}