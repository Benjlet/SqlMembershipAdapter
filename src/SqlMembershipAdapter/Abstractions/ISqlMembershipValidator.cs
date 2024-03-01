namespace SqlMembershipAdapter.Abstractions
{
    internal interface ISqlMembershipValidator
    {
        bool ValidateEmail(string? email);
        bool ValidatePageRange(int pageIndex, int pageSize);
        bool ValidatePassword(string? password);
        bool ValidatePasswordAnswer(string? passwordAnswer);
        bool ValidatePasswordComplexity(string? password);
        bool ValidatePasswordQuestion(string? passwordQuestion);
        bool ValidateUsername(string? username);
    }
}