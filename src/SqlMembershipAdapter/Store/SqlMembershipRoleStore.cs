using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Exceptions;

namespace SqlMembershipAdapter.Store
{
    internal class SqlMembershipRoleStore : ISqlMembershipRoleStore
    {
        private readonly ISqlMembershipSettings _settings;

        public SqlMembershipRoleStore(
            ISqlMembershipSettings settings)
        {
            _settings = settings;
        }

        public async Task<bool> IsUserInRole(string userName, string roleName)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_UsersInRoles_IsUserInRole", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, userName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int iStatus = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            return iStatus switch
            {
                0 => false,
                1 => true,
                2 => false,
                3 => false,
                _ => throw new ProviderException("Stored procedure call failed.")
            };
        }

        public async Task<string[]> GetRolesForUser(string username)
        {
            StringCollection sc = new();

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_UsersInRoles_GetRolesForUser", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
            {
                if (await reader.ReadAsync())
                {
                    sc.Add(reader.GetString(0));
                }
            }

            int status = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (sc.Count > 0)
            {
                string[] strReturn = new string[sc.Count];
                sc.CopyTo(strReturn, 0);
                return strReturn;
            }

            return status switch
            {
                0 => Array.Empty<string>(),
                1 => Array.Empty<string>(),
                _ => throw new ProviderException("Stored procedure call failed.")
            };
        }

        public async Task CreateRole(string roleName)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Roles_CreateRole", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int iStatus = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (iStatus == 0)
            {
                return;
            }

            throw iStatus switch
            {
                1 => new ProviderException(string.Format("The role '{0}' already exists.", roleName)),
                _ => new ProviderException("Stored procedure call failed."),
            };
        }

        public async Task<bool> DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Roles_DeleteRole", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));
            command.Parameters.Add(CreateInputParam("@DeleteOnlyIfRoleIsEmpty", SqlDbType.Bit, throwOnPopulatedRole ? 1 : 0));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int iStatus = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (iStatus == 2)
            {
                throw new ProviderException("This role cannot be deleted because there are users present in it.");
            }

            return iStatus == 0;
        }

        public async Task<bool> RoleExists(string roleName)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Roles_RoleExists", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int iStatus = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            return iStatus switch
            {
                0 => false,
                1 => true,
                _ => throw new ProviderException("Stored procedure call failed.")
            };
        }

        public async Task<string[]> GetUsersInRole(string roleName)
        {
            StringCollection sc = new();

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_UsersInRoles_GetUsersInRoles", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
            {
                if (await reader.ReadAsync())
                {
                    sc.Add(reader.GetString(0));
                }
            }

            int status = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (sc.Count < 1)
            {
                return status switch
                {
                    0 => Array.Empty<string>(),
                    1 => throw new ProviderException(string.Format("The role '{0}' was not found.", roleName)),
                    _ => throw new ProviderException("Stored procedure call failed."),
                };
            }

            string[] strReturn = new string[sc.Count];
            sc.CopyTo(strReturn, 0);
            return strReturn;
        }

        public async Task<string[]> GetAllRoles()
        {
            StringCollection sc = new();

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Roles_GetAllRoles", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
            {
                if (await reader.ReadAsync())
                {
                    sc.Add(reader.GetString(0));
                }
            }

            string[] strReturn = new string[sc.Count];
            sc.CopyTo(strReturn, 0);
            return strReturn;
        }

        public async Task<string[]> FindUsersInRole(string roleName, string usernameToMatch)
        {
            StringCollection sc = new();

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_UsersInRoles_FindUsersInRole", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleName", SqlDbType.NVarChar, roleName));
            command.Parameters.Add(CreateInputParam("@UserNameToMatch", SqlDbType.NVarChar, usernameToMatch));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
            {
                if (await reader.ReadAsync())
                {
                    sc.Add(reader.GetString(0));
                }
            }

            int status = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (sc.Count < 1)
            {
                return status switch
                {
                    0 => Array.Empty<string>(),
                    1 => throw new ProviderException(string.Format("The role '{0}' was not found.", roleName)),
                    _ => throw new ProviderException("Stored procedure call failed.")
                };
            }

            string[] strReturn = new string[sc.Count];
            sc.CopyTo(strReturn, 0);
            return strReturn;
        }

        public async Task AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            bool beginTranCalled = false;

            try
            {
                using SqlConnection connection = new(_settings.ConnectionString);
                await connection.OpenAsync();

                try
                {
                    int numUsersRemaing = usernames.Length;

                    while (numUsersRemaing > 0)
                    {
                        int iter;
                        string allUsers = usernames[^numUsersRemaing];
                        numUsersRemaing--;

                        for (iter = usernames.Length - numUsersRemaing; iter < usernames.Length; iter++)
                        {
                            if (allUsers.Length + usernames[iter].Length + 1 >= 4000)
                            {
                                break;
                            }

                            allUsers += "," + usernames[iter];
                            numUsersRemaing--;
                        }

                        int numRolesRemaining = roleNames.Length;

                        while (numRolesRemaining > 0)
                        {
                            string allRoles = roleNames[^numRolesRemaining];
                            numRolesRemaining--;

                            for (iter = roleNames.Length - numRolesRemaining; iter < roleNames.Length; iter++)
                            {
                                if (allRoles.Length + roleNames[iter].Length + 1 >= 4000)
                                {
                                    break;
                                }

                                allRoles += "," + roleNames[iter];
                                numRolesRemaining--;
                            }

                            if (!beginTranCalled && (numUsersRemaing > 0 || numRolesRemaining > 0))
                            {
                                await new SqlCommand("BEGIN TRANSACTION", connection).ExecuteNonQueryAsync();
                                beginTranCalled = true;
                            }

                            await AddUsersToRolesCore(connection, allUsers, allRoles);
                        }
                    }

                    if (beginTranCalled)
                    {
                        await new SqlCommand("COMMIT TRANSACTION", connection).ExecuteNonQueryAsync();
                        beginTranCalled = false;
                    }
                }
                catch
                {
                    if (beginTranCalled)
                    {
                        try
                        {
                            await new SqlCommand("ROLLBACK TRANSACTION", connection).ExecuteNonQueryAsync();
                        }
                        catch
                        {
                        }

                        beginTranCalled = false;
                    }

                    throw;
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task AddUsersToRolesCore(SqlConnection connection, string usernames, string roleNames)
        {
            string user = string.Empty, roleName = string.Empty;

            using SqlCommand command = new("dbo.aspnet_UsersInRoles_AddUsersToRoles", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleNames", SqlDbType.NVarChar, roleNames));
            command.Parameters.Add(CreateInputParam("@UserNames", SqlDbType.NVarChar, usernames));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (await reader.ReadAsync())
                {
                    if (reader.FieldCount > 0)
                    {
                        user = reader.GetString(0);
                    }

                    if (reader.FieldCount > 1)
                    {
                        roleName = reader.GetString(1);
                    }
                }
            }

            int status = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (status == 0)
            {
                return;
            }

            throw status switch
            {
                1 => new ProviderException(string.Format("The user '{0}' was not found.", user)),
                2 => new ProviderException(string.Format("The role '{0}' was not found.", roleName)),
                3 => new ProviderException(string.Format("The user '{0}' is already in role '{1}'.", user, roleName)),
                _ => new ProviderException("Stored procedure call failed."),
            };
        }

        public async Task RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            bool beginTranCalled = false;

            try
            {
                using SqlConnection connection = new(_settings.ConnectionString);
                await connection.OpenAsync();

                try
                {
                    int numUsersRemaing = usernames.Length;

                    while (numUsersRemaing > 0)
                    {
                        int i;
                        string allUsers = usernames[^numUsersRemaing];
                        numUsersRemaing--;
                        for (i = usernames.Length - numUsersRemaing; i < usernames.Length; i++)
                        {
                            if (allUsers.Length + usernames[i].Length + 1 >= 4000)
                            {
                                break;
                            }

                            allUsers += "," + usernames[i];
                            numUsersRemaing--;
                        }

                        int numRolesRemaining = roleNames.Length;
                        while (numRolesRemaining > 0)
                        {
                            string allRoles = roleNames[^numRolesRemaining];
                            numRolesRemaining--;
                            for (i = roleNames.Length - numRolesRemaining; i < roleNames.Length; i++)
                            {
                                if (allRoles.Length + roleNames[i].Length + 1 >= 4000)
                                {
                                    break;
                                }

                                allRoles += "," + roleNames[i];
                                numRolesRemaining--;
                            }

                            if (!beginTranCalled && (numUsersRemaing > 0 || numRolesRemaining > 0))
                            {
                                await new SqlCommand("BEGIN TRANSACTION", connection).ExecuteNonQueryAsync();
                                beginTranCalled = true;
                            }

                            await RemoveUsersFromRolesCore(connection, allUsers, allRoles);
                        }
                    }
                    if (beginTranCalled)
                    {
                        await new SqlCommand("COMMIT TRANSACTION", connection).ExecuteNonQueryAsync();
                        beginTranCalled = false;
                    }
                }
                catch
                {
                    if (beginTranCalled)
                    {
                        await new SqlCommand("ROLLBACK TRANSACTION", connection).ExecuteNonQueryAsync();
                        beginTranCalled = false;
                    }

                    throw;
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task RemoveUsersFromRolesCore(SqlConnection connection, string usernames, string roleNames)
        {
            string user = string.Empty, roleName = string.Empty;

            using SqlCommand command = new("dbo.aspnet_UsersInRoles_RemoveUsersFromRoles", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = TimeSpan.FromMilliseconds(_settings.CommandTimeoutMilliseconds).Seconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@RoleNames", SqlDbType.NVarChar, roleNames));
            command.Parameters.Add(CreateInputParam("@UserNames", SqlDbType.NVarChar, usernames));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (await reader.ReadAsync())
                {
                    if (reader.FieldCount > 0)
                    {
                        user = reader.GetString(0);
                    }

                    if (reader.FieldCount > 1)
                    {
                        roleName = reader.GetString(1);
                    }
                }
            }

            int status = returnParameter?.Value != null ? (int)returnParameter.Value : -1;

            if (status == 0)
            {
                return;
            }

            throw status switch
            {
                1 => new ProviderException(string.Format("The user '{0}' was not found.", user)),
                2 => new ProviderException(string.Format("The role '{0}' was not found.", roleName)),
                3 => new ProviderException(string.Format("The user '{0}' is already not in role '{1}'.", user, roleName)),
                _ => new ProviderException("Stored procedure call failed."),
            };
        }

        private static SqlParameter CreateInputParam(string? paramName, SqlDbType dbType, object? objValue)
        {
            SqlParameter param = new(paramName, dbType);

            if (objValue == null)
            {
                param.IsNullable = true;
                param.Value = DBNull.Value;
            }
            else
            {
                param.Value = objValue;
            }

            return param;
        }
    }
}