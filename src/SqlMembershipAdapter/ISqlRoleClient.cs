using SqlMembershipAdapter.Exceptions;

namespace SqlMembershipAdapter
{
    /// <summary>
    /// Adapter for SQL Role Provider calls.
    /// </summary>
    public interface ISqlRoleClient
    {
        /// <summary>
        /// Adds users to all supplied roles.
        /// </summary>
        /// <param name="usernames">Usernames.</param>
        /// <param name="roleNames">Role names.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task AddUsersToRoles(string[] usernames, string[] roleNames);

        /// <summary>
        /// Creates a role with the supplied name.
        /// </summary>
        /// <param name="roleName">Role name.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task CreateRole(string roleName);

        /// <summary>
        /// Deletes a role matching the supplied name.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <param name="throwOnPopulatedRole">Whether a <see cref="ProviderException"/> should be thrown if the role is populated.</param>
        /// <returns>Outcome of the deletion.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<bool> DeleteRole(string roleName, bool throwOnPopulatedRole);

        /// <summary>
        /// Finds users in a specific role.
        /// </summary>
        /// <param name="roleName">Role name.</param>
        /// <param name="usernameToMatch">Username.</param>
        /// <returns>All matching users in the role.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<string[]> FindUsersInRole(string roleName, string usernameToMatch);

        /// <summary>
        /// Get all role names.
        /// </summary>
        /// <returns>All configured role names.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<string[]> GetAllRoles();

        /// <summary>
        /// Gets all roles the user is assigned to.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <returns>All roles the user is assigned to.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<string[]> GetRolesForUser(string username);

        /// <summary>
        /// Gets all users in a specific role.
        /// </summary>
        /// <param name="roleName">Role name.</param>
        /// <returns>All users in the supplied role.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<string[]> GetUsersInRole(string roleName);

        /// <summary>
        /// Determines if a user is assigned to a specific role.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="roleName">Role name.</param>
        /// <returns>Whether the user is assigned the role or not.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<bool> IsUserInRole(string username, string roleName);

        /// <summary>
        /// Removes the user from the supplied roles.
        /// </summary>
        /// <param name="usernames">Users to remove.</param>
        /// <param name="roleNames">The roles to remove the users from.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task RemoveUsersFromRoles(string[] usernames, string[] roleNames);

        /// <summary>
        /// Determines whether a role exists or not.
        /// </summary>
        /// <param name="roleName">Role name.</param>
        /// <returns>Whether the role exists or not.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ProviderException"/>
        Task<bool> RoleExists(string roleName);
    }
}