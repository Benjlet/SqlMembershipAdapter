using SqlMembershipAdapter.Exceptions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;

namespace SqlMembershipAdapter.Abstractions
{
    /// <summary>
    /// Adapter for SQL Membership calls.
    /// </summary>
    internal interface ISqlMembership
    {
        /// <summary>
        /// Validates and changes the Membership user's password based on the supplied request details.
        /// </summary>
        /// <param name="request">Request details.</param>
        /// <returns>Status of the password change.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="MembershipPasswordException"/>
        /// <exception cref="ProviderException"/>
        Task<bool> ChangePassword(ChangePasswordRequest request);

        /// <summary>
        /// Validates and changes the Membership user's password question and answer based on the supplied request details.
        /// </summary>
        /// <param name="request">Request details.</param>
        /// <returns>Status of the password question and answer change.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<bool> ChangePasswordQuestionAndAnswer(ChangePasswordQuestionAndAnswerRequest request);

        /// <summary>
        /// Creates a new user in the SQL Membership tables.
        /// </summary>
        /// <param name="request">New user request details.</param>
        /// <returns>The outcome of the user creation attempt.</returns>
        Task<CreateUserResult> CreateUser(CreateUserRequest request);

        /// <summary>
        /// Deletes the requested Membership user.
        /// </summary>
        /// <param name="request">Details of the user to delete.</param>
        /// <returns>Outcome of the user deletion attempt.</returns>
        /// <exception cref="ArgumentException"/>
        Task<bool> DeleteUser(DeleteUserRequest request);

        /// <summary>
        /// Finds users by the specified email.
        /// </summary>
        /// <param name="request">Search request details.</param>
        /// <returns>Collection of users matching the search criteria.</returns>
        /// <exception cref="ArgumentException"/>
        Task<MembershipUserCollection> FindUsersByEmail(FindUsersRequest request);

        /// <summary>
        /// Finds users by the specified username.
        /// </summary>
        /// <param name="request">Search request details.</param>
        /// <returns>Collection of users matching the search criteria.</returns>
        /// <exception cref="ArgumentException"/>
        Task<MembershipUserCollection> FindUsersByName(FindUsersRequest request);

        /// <summary>
        /// Generates a password that meets the configured length and complexity requirements.
        /// </summary>
        /// <returns>The generated password.</returns>
        Task<string> GeneratePassword();

        /// <summary>
        /// Gets details of all Membership users within an page range.
        /// </summary>
        /// <param name="pageIndex">The index of the results page.</param>
        /// <param name="pageSize">The size of the results page.</param>
        /// <returns>Collection of users matching the search criteria.</returns>
        /// <exception cref="ArgumentException"/>
        Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize);

        /// <summary>
        /// Gets the number of users that have logged in within the last N minutes where N is the supplied time window.
        /// </summary>
        /// <param name="timeWindowMinutes">The time (in minutes) since the user last logged in.</param>
        /// <returns>The count of users that logged in within the supplied time window.</returns>
        Task<int> GetNumberOfUsersOnline(int timeWindowMinutes);

        /// <summary>
        /// Gets details of a specific Membership user by their username.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="updateLastActivity">Flag for whether to treat this as a user activity.</param>
        /// <returns>Details of the Membership user.</returns>
        /// <exception cref="ArgumentException"/>
        Task<MembershipUser?> GetUser(string username, bool updateLastActivity);

        /// <summary>
        /// Gets the username associated with the supplied email.
        /// </summary>
        /// <param name="email">Email.</param>
        /// <returns>The associated username.</returns>
        /// <exception cref="ArgumentException"/>
        Task<string?> GetUserNameByEmail(string email);

        /// <summary>
        /// Resets the Membership user matching the supplied request details.
        /// </summary>
        /// <param name="request">Reset password request details.</param>
        /// <returns>The temporary password.</returns>
        /// <exception cref="ArgumentException"/>
        Task<string> ResetPassword(ResetPasswordRequest request);

        /// <summary>
        /// Unlocks the Membership user associated with the supplied username.
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>The outcome of the unlock request.</returns>
        /// <exception cref="ArgumentException"/>
        Task<bool> UnlockUser(string username);

        /// <summary>
        /// Updates the supplied Membership user details.
        /// </summary>
        /// <param name="user">Details of the updated Membership user.</param>
        /// <exception cref="ArgumentException"/>
        Task UpdateUser(MembershipUser user);

        /// <summary>
        /// Validates the supplied user details.
        /// </summary>
        /// <param name="request">Details of the Membership user to validate.</param>
        /// <returns>Outcome of user validation.</returns>
        /// <exception cref="ArgumentException"/>
        Task<bool> ValidateUser(ValidateUserRequest request);
    }
}