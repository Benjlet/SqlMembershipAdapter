namespace SqlMembershipAdapter.Models
{
    public class CheckPasswordResult
    {
        private string? _passwordSalt = null;

        public int PasswordFormat { get; set; }
        public bool IsValid { get; set; }
        public string? PasswordSalt
        {
            get => _passwordSalt;
            set => _passwordSalt = value;
        }
    }
}