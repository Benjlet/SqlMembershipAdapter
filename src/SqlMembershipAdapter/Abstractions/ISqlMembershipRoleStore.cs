namespace SqlMembershipAdapter.Abstractions
{
    internal interface ISqlMembershipRoleStore
    {
        Task AddUsersToRoles(string[] usernames, string[] roleNames);
        Task CreateRole(string roleName);
        Task<bool> DeleteRole(string roleName, bool throwOnPopulatedRole);
        Task<string[]> FindUsersInRole(string roleName, string usernameToMatch);
        Task<string[]> GetAllRoles();
        Task<string[]> GetRolesForUser(string username);
        Task<string[]> GetUsersInRole(string roleName);
        Task<bool> IsUserInRole(string userName, string roleName);
        Task RemoveUsersFromRoles(string[] usernames, string[] roleNames);
        Task<bool> RoleExists(string roleName);
    }
}