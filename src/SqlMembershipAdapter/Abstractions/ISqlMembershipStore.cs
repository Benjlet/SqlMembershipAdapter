using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Abstractions
{
    internal interface ISqlMembershipStore
    {
        Task<bool> ChangePassword(string username, string newPassword, string? passwordSalt, int passwordFormat);
        Task ChangePasswordQuestionAndAnswer(string username, string password, string? newPasswordQuestion, string? newPasswordAnswer);
        Task CheckPassword(string username, bool isPasswordCorrect, bool updateLastLoginActivityDate, DateTime lastLoginDate, DateTime lastActivityDate);
        Task<CreateUserResult> CreateUser(Guid? providerUserKey, string? userName, string? password, string? passwordSalt, string? email, string? passwordQuestion, string? passwordAnswer, bool isApproved);
        Task<bool> DeleteUser(string username, bool deleteAllRelatedData);
        Task<MembershipUserCollection> FindUsersByEmail(string? emailToMatch, int pageIndex, int pageSize);
        Task<MembershipUserCollection> FindUsersByName(string usernameToMatch, int pageIndex, int pageSize);
        Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize);
        Task<int> GetNumberOfUsersOnline(int timeWindowMinutes);
        Task<GetPasswordWithFormatResult> GetPasswordWithFormat(string username, bool updateLastLoginActivityDate);
        Task<MembershipUser?> GetUser(Guid providerUserKey, bool userIsOnline);
        Task<MembershipUser?> GetUser(string username, bool updateLastActivity);
        Task<string?> GetUsernameByEmail(string? email);
        Task ResetPassword(string username, string newPasswordEncoded, string? passwordSalt, string? passwordAnswer, int passwordFormat);
        Task<bool> UnlockUser(string userName);
        Task UpdateUser(MembershipUser user);
    }
}