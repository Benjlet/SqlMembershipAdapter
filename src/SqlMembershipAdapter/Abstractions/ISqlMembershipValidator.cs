using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;

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
        bool ValidateCreateUserRequest(CreateUserRequest request, out MembershipCreateStatus status);
        bool ValidateChangePasswordQuestionAnswer(ChangePasswordQuestionAndAnswerRequest request, out string? invalidParam);
        bool ValidateChangePasswordRequest(ChangePasswordRequest request, out string? invalidParam);
    }
}