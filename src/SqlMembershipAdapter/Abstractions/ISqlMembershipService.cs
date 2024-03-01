using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter.Abstractions
{
    internal interface ISqlMembershipService
    {
        Task<bool> ChangePassword(ChangePasswordRequest request);
        Task<bool> ChangePasswordQuestionAndAnswer(ChangePasswordQuestionAndAnswerRequest request);
        Task<CreateUserResult> CreateUser(CreateUserRequest request);
        Task<bool> DeleteUser(DeleteUserRequest request);
        Task<MembershipUserCollection> FindUsersByEmail(FindUsersRequest request);
        Task<MembershipUserCollection> FindUsersByName(FindUsersRequest request);
        Task<string> GeneratePassword();
        Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize);
        Task<int> GetNumberOfUsersOnline(int timeWindowMinutes);
        Task<MembershipUser?> GetUser(string username, bool updateLastActivity);
        Task<string?> GetUserNameByEmail(string email);
        Task<string> ResetPassword(ResetPasswordRequest request);
        Task<bool> UnlockUser(string username);
        Task UpdateUser(MembershipUser user);
        Task<bool> ValidateUser(ValidateUserRequest request);
    }
}