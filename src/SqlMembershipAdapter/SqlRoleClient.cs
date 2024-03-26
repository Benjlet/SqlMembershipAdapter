using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Implementation;
using SqlMembershipAdapter.Store;

namespace SqlMembershipAdapter
{
    /// <summary>
    /// Adapter for SqlRoleProvider calls to Membership.
    /// </summary>
    public class SqlRoleClient : ISqlRoleClient
    {
        private readonly ISqlMembershipRoleStore _sqlStore;
        private readonly ISqlMembershipSettings _settings;
        private readonly ISqlMembershipValidator _validator;

        /// <summary>
        /// Initialises a new SqlRoleClient with the supplied connection string.
        /// </summary>
        /// <param name="sqlConnectionString">Membership database connection string.</param>
        public SqlRoleClient(
            string sqlConnectionString)
        {
            _settings = new SqlMembershipSettings(sqlConnectionString);
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipRoleStore(_settings);
        }

        /// <summary>
        /// Initialises a new SqlRoleClient with the supplied settings.
        /// </summary>
        /// <param name="settings">Membership settings.</param>
        /// <exception cref="ArgumentNullException"/>
        public SqlRoleClient(
            SqlMembershipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipRoleStore(_settings);
        }

        /// <summary>
        /// Internal implementation for unit testing.
        /// </summary>
        internal SqlRoleClient(
            ISqlMembershipRoleStore sqlStore,
            ISqlMembershipValidator validator,
            ISqlMembershipSettings settings)
        {
            _settings = settings;
            _sqlStore = sqlStore;
            _validator = validator;
        }

        /// <inheritdoc/>
        public async Task<bool> IsUserInRole(string username, string roleName)
        {
            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            bool isUserInRole = await _sqlStore.IsUserInRole(username, roleName).ConfigureAwait(false);

            return isUserInRole;
        }

        /// <inheritdoc/>
        public async Task<string[]> GetRolesForUser(string username)
        {
            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            string[] roles = await _sqlStore.GetRolesForUser(username).ConfigureAwait(false);

            return roles;
        }

        /// <inheritdoc/>
        public async Task CreateRole(string roleName)
        {
            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            await _sqlStore.CreateRole(roleName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            return await _sqlStore.DeleteRole(roleName, throwOnPopulatedRole).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> RoleExists(string roleName)
        {
            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            return await _sqlStore.RoleExists(roleName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string[]> GetUsersInRole(string roleName)
        {
            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            return await _sqlStore.GetUsersInRole(roleName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string[]> GetAllRoles()
        {
            return await _sqlStore.GetAllRoles();
        }

        /// <inheritdoc/>
        public async Task<string[]> FindUsersInRole(string roleName, string usernameToMatch)
        {
            if (string.IsNullOrWhiteSpace(usernameToMatch))
            {
                throw new ArgumentException(nameof(usernameToMatch));
            }

            if (!_validator.ValidateRoleName(roleName))
            {
                throw new ArgumentException(nameof(roleName));
            }

            return await _sqlStore.FindUsersInRole(roleName, usernameToMatch).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (!_validator.ValidateArray(roleNames))
            {
                throw new ArgumentException(nameof(roleNames));
            }

            if (!_validator.ValidateArray(usernames))
            {
                throw new ArgumentException(nameof(usernames));
            }

            await _sqlStore.AddUsersToRoles(usernames, roleNames).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if (!_validator.ValidateArray(roleNames))
            {
                throw new ArgumentException(nameof(roleNames));
            }

            if (!_validator.ValidateArray(usernames))
            {
                throw new ArgumentException(nameof(usernames));
            }

            await _sqlStore.RemoveUsersFromRoles(usernames, roleNames).ConfigureAwait(false);
        }
    }
}
