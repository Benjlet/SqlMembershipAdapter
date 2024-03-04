namespace SqlMembershipAdapter.Abstractions
{
    internal interface ISqlMembershipEncryption
    {
        string? Encode(string? text, int format, string? salt);
        string GeneratePassword();
        string GenerateSalt();
    }
}