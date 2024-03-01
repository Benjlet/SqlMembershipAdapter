using System.Data;
using System.Data.SqlClient;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter
{
    internal class SqlMembershipStore : ISqlMembershipStore
    {
        private readonly ISqlMembershipSettings _settings;

        public SqlMembershipStore(
            ISqlMembershipSettings settings)
        {
            _settings = settings;
        }

        public async Task<CreateUserResult> CreateUser(
            Guid? providerUserKey,
            string? userName,
            string? password,
            string? passwordSalt,
            string? email,
            string? passwordQuestion,
            string? passwordAnswer,
            bool isApproved)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    DateTime currentTime = RoundToSeconds(DateTime.UtcNow);

                    SqlCommand cmd = new("dbo.aspnet_Membership_CreateUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, userName));
                    cmd.Parameters.Add(CreateInputParam("@Password", SqlDbType.NVarChar, password));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
                    cmd.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, email));
                    cmd.Parameters.Add(CreateInputParam("@PasswordQuestion", SqlDbType.NVarChar, passwordQuestion));
                    cmd.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, passwordAnswer));
                    cmd.Parameters.Add(CreateInputParam("@IsApproved", SqlDbType.Bit, isApproved));
                    cmd.Parameters.Add(CreateInputParam("@UniqueEmail", SqlDbType.Int, _settings.RequiresUniqueEmail ? 1 : 0));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, (int)_settings.PasswordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, currentTime));

                    SqlParameter p = CreateInputParam("@UserId", SqlDbType.UniqueIdentifier, providerUserKey);
                    p.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters.Add(p);

                    p = new SqlParameter("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException sqlEx)
                    {
                        if (sqlEx.Number == 2627 || sqlEx.Number == 2601 || sqlEx.Number == 2512)
                        {
                            return new CreateUserResult(MembershipCreateStatus.DuplicateUserName);
                        }

                        throw;
                    }

                    int iStatus = (p.Value != null) ? ((int)p.Value) : -1;

                    if (iStatus < 0 || iStatus > (int)MembershipCreateStatus.ProviderError)
                    {
                        iStatus = (int)MembershipCreateStatus.ProviderError;
                    }

                    MembershipCreateStatus status = (MembershipCreateStatus)iStatus;

                    if (iStatus != 0)
                    {
                        return new CreateUserResult(status);
                    }

                    string? userIdText = cmd.Parameters["@UserId"].Value.ToString();

                    Guid? userId = !string.IsNullOrWhiteSpace(userIdText) ? new Guid(userIdText) : null;

                    currentTime = currentTime.ToLocalTime();

                    MembershipUser user = new(
                        providerName: _settings.ApplicationName,
                        userName: userName,
                        providerUserKey: userId,
                        email: email,
                        passwordQuestion: passwordQuestion,
                        comment: null,
                        isApproved: isApproved,
                        isLockedOut: false,
                        creationDate: currentTime,
                        lastLoginDate: currentTime,
                        lastActivityDate: currentTime,
                        lastPasswordChangedDate: currentTime,
                        lastLockoutDate: new DateTime(1754, 1, 1));

                    return new CreateUserResult(user, status);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> ChangePasswordQuestionAndAnswer(string username, string password, string? newPasswordQuestion, string? newPasswordAnswer)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_ChangePasswordQuestionAndAnswer", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPasswordQuestion", SqlDbType.NVarChar, newPasswordQuestion));
                    cmd.Parameters.Add(CreateInputParam("@NewPasswordAnswer", SqlDbType.NVarChar, newPasswordAnswer));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    await cmd.ExecuteNonQueryAsync();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        throw new ProviderException(GetExceptionText(status));
                    }

                    return status == 0;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<MembershipUserCollection> FindUsersByName(string usernameToMatch, int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_FindUsersByName", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserNameToMatch", SqlDbType.NVarChar, usernameToMatch));
                    cmd.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
                    cmd.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    try
                    {
                        using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                        while (await reader.ReadAsync())
                        {
                            string? username, email, passwordQuestion, comment;
                            bool isApproved;
                            DateTime dtCreate, dtLastLogin, dtLastActivity, dtLastPassChange;
                            Guid userId;
                            bool isLockedOut;
                            DateTime dtLastLockoutDate;

                            username = GetNullableString(reader, 0);
                            email = GetNullableString(reader, 1);
                            passwordQuestion = GetNullableString(reader, 2);
                            comment = GetNullableString(reader, 3);
                            isApproved = reader.GetBoolean(4);
                            dtCreate = reader.GetDateTime(5).ToLocalTime();
                            dtLastLogin = reader.GetDateTime(6).ToLocalTime();
                            dtLastActivity = reader.GetDateTime(7).ToLocalTime();
                            dtLastPassChange = reader.GetDateTime(8).ToLocalTime();
                            userId = reader.GetGuid(9);
                            isLockedOut = reader.GetBoolean(10);
                            dtLastLockoutDate = reader.GetDateTime(11).ToLocalTime();

                            users.Add(new MembershipUser(
                                providerName: _settings.ApplicationName,
                                userName: username,
                                providerUserKey: userId,
                                email: email,
                                passwordQuestion: passwordQuestion,
                                comment: comment,
                                isApproved: isApproved,
                                isLockedOut: isLockedOut,
                                creationDate: dtCreate,
                                lastLoginDate: dtLastLogin,
                                lastActivityDate: dtLastActivity,
                                lastPasswordChangedDate: dtLastPassChange,
                                lastLockoutDate: dtLastLockoutDate));
                        }
                    }
                    finally
                    {
                        if (p.Value != null && p.Value is int)
                        {
                            totalRecords = (int)p.Value;
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<MembershipUserCollection> FindUsersByEmail(string? emailToMatch, int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_FindUsersByEmail", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@EmailToMatch", SqlDbType.NVarChar, emailToMatch));
                    cmd.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
                    cmd.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));
                    cmd.Parameters.Add(p);

                    try
                    {
                        using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                        while (await reader.ReadAsync())
                        {
                            string? username, email, passwordQuestion, comment;
                            bool isApproved;
                            DateTime dtCreate, dtLastLogin, dtLastActivity, dtLastPassChange;
                            Guid userId;
                            bool isLockedOut;
                            DateTime dtLastLockoutDate;

                            username = GetNullableString(reader, 0);
                            email = GetNullableString(reader, 1);
                            passwordQuestion = GetNullableString(reader, 2);
                            comment = GetNullableString(reader, 3);
                            isApproved = reader.GetBoolean(4);
                            dtCreate = reader.GetDateTime(5).ToLocalTime();
                            dtLastLogin = reader.GetDateTime(6).ToLocalTime();
                            dtLastActivity = reader.GetDateTime(7).ToLocalTime();
                            dtLastPassChange = reader.GetDateTime(8).ToLocalTime();
                            userId = reader.GetGuid(9);
                            isLockedOut = reader.GetBoolean(10);
                            dtLastLockoutDate = reader.GetDateTime(11).ToLocalTime();

                            users.Add(new MembershipUser(
                                providerName: _settings.ApplicationName,
                                userName: username,
                                providerUserKey: userId,
                                email: email,
                                passwordQuestion: passwordQuestion,
                                comment: comment,
                                isApproved: isApproved,
                                isLockedOut: isLockedOut,
                                creationDate: dtCreate,
                                lastLoginDate: dtLastLogin,
                                lastActivityDate: dtLastActivity,
                                lastPasswordChangedDate: dtLastPassChange,
                                lastLockoutDate: dtLastLockoutDate));
                        }
                    }
                    finally
                    {
                        if (p.Value != null && p.Value is int)
                        {
                            totalRecords = (int)p.Value;
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<bool> ChangePassword(string username, string newPassword, string? passwordSalt, int passwordFormat)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                int status;

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_SetPassword", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, newPassword));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, passwordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    await cmd.ExecuteNonQueryAsync();

                    status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        string errText = GetExceptionText(status);

                        if (IsStatusDueToBadPassword(status))
                        {
                            throw new MembershipPasswordException(errText);
                        }
                        else
                        {
                            throw new ProviderException(errText);
                        }
                    }

                    return true;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task UpdateUser(MembershipUser user)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_UpdateUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, user.UserName));
                    cmd.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, user.Email));
                    cmd.Parameters.Add(CreateInputParam("@Comment", SqlDbType.NText, user.Comment));
                    cmd.Parameters.Add(CreateInputParam("@IsApproved", SqlDbType.Bit, user.IsApproved ? 1 : 0));
                    cmd.Parameters.Add(CreateInputParam("@LastLoginDate", SqlDbType.DateTime, user.LastLoginDate.ToUniversalTime()));
                    cmd.Parameters.Add(CreateInputParam("@LastActivityDate", SqlDbType.DateTime, user.LastActivityDate.ToUniversalTime()));
                    cmd.Parameters.Add(CreateInputParam("@UniqueEmail", SqlDbType.Int, _settings.RequiresUniqueEmail ? 1 : 0));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);
                    await cmd.ExecuteNonQueryAsync();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        throw new ProviderException(GetExceptionText(status));
                    }

                    return;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> UnlockUser(string userName)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_UnlockUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, userName));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    await cmd.ExecuteNonQueryAsync();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    return status == 0;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<MembershipUser?> GetUser(string username, bool updateLastActivity)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetUserByName", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastActivity", SqlDbType.Bit, updateLastActivity));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        string? email = GetNullableString(reader, 0);
                        string? passwordQuestion = GetNullableString(reader, 1);
                        string? comment = GetNullableString(reader, 2);
                        bool isApproved = reader.GetBoolean(3);
                        DateTime dtCreate = reader.GetDateTime(4).ToLocalTime();
                        DateTime dtLastLogin = reader.GetDateTime(5).ToLocalTime();
                        DateTime dtLastActivity = reader.GetDateTime(6).ToLocalTime();
                        DateTime dtLastPassChange = reader.GetDateTime(7).ToLocalTime();
                        Guid userId = reader.GetGuid(8);
                        bool isLockedOut = reader.GetBoolean(9);
                        DateTime dtLastLockoutDate = reader.GetDateTime(10).ToLocalTime();

                        return new MembershipUser(
                            providerName: _settings.ApplicationName,
                            userName: username,
                            providerUserKey: userId,
                            email: email,
                            passwordQuestion: passwordQuestion,
                            comment: comment,
                            isApproved: isApproved,
                            isLockedOut: isLockedOut,
                            creationDate: dtCreate,
                            lastLoginDate: dtLastLogin,
                            lastActivityDate: dtLastActivity,
                            lastPasswordChangedDate: dtLastPassChange,
                            lastLockoutDate: dtLastLockoutDate);
                    }

                    return null;

                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> GetNumberOfUsersOnline(int timeWindowMinutes)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetNumberOfUsersOnline", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@MinutesSinceLastInActive", SqlDbType.Int, timeWindowMinutes));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);
                    await cmd.ExecuteNonQueryAsync();
                    int num = (p.Value != null) ? ((int)p.Value) : -1;
                    return num;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetAllUsers", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
                    cmd.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    try
                    {
                        using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                        while (await reader.ReadAsync())
                        {
                            string? username, email, passwordQuestion, comment;
                            bool isApproved;
                            DateTime dtCreate, dtLastLogin, dtLastActivity, dtLastPassChange;
                            Guid userId;
                            bool isLockedOut;
                            DateTime dtLastLockoutDate;

                            username = GetNullableString(reader, 0);
                            email = GetNullableString(reader, 1);
                            passwordQuestion = GetNullableString(reader, 2);
                            comment = GetNullableString(reader, 3);
                            isApproved = reader.GetBoolean(4);
                            dtCreate = reader.GetDateTime(5).ToLocalTime();
                            dtLastLogin = reader.GetDateTime(6).ToLocalTime();
                            dtLastActivity = reader.GetDateTime(7).ToLocalTime();
                            dtLastPassChange = reader.GetDateTime(8).ToLocalTime();
                            userId = reader.GetGuid(9);
                            isLockedOut = reader.GetBoolean(10);
                            dtLastLockoutDate = reader.GetDateTime(11).ToLocalTime();

                            users.Add(new MembershipUser(
                                providerName: _settings.ApplicationName,
                                userName: username,
                                providerUserKey: userId,
                                email: email,
                                passwordQuestion: passwordQuestion,
                                comment: comment,
                                isApproved: isApproved,
                                isLockedOut: isLockedOut,
                                creationDate: dtCreate,
                                lastLoginDate: dtLastLogin,
                                lastActivityDate: dtLastActivity,
                                lastPasswordChangedDate: dtLastPassChange,
                                lastLockoutDate: dtLastLockoutDate));
                        }
                    }
                    finally
                    {
                        if (p.Value is not null and int)
                        {
                            totalRecords = (int)p.Value;
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<bool> DeleteUser(string username, bool deleteAllRelatedData)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Users_DeleteUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@TablesToDeleteFrom", SqlDbType.Int, deleteAllRelatedData ? 0xF : 1));

                    SqlParameter p = new("@NumTablesDeletedFrom", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    cmd.Parameters.Add(p);
                    await cmd.ExecuteNonQueryAsync();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    return status > 0;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<string?> GetUsernameByEmail(string? email)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetUserByEmail", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, email));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    string? username = null;

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                    if (await reader.ReadAsync())
                    {
                        username = GetNullableString(reader, 0);

                        if (_settings.RequiresUniqueEmail && await reader.ReadAsync())
                        {
                            throw new ProviderException("More than one user has the specified e-mail address.");
                        }
                    }

                    return username;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<MembershipUser?> GetUser(Guid providerUserKey, bool userIsOnline)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetUserByUserId", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@UserId", SqlDbType.UniqueIdentifier, providerUserKey));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastActivity", SqlDbType.Bit, userIsOnline));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        string? email = GetNullableString(reader, 0);
                        string? passwordQuestion = GetNullableString(reader, 1);
                        string? comment = GetNullableString(reader, 2);
                        bool isApproved = reader.GetBoolean(3);
                        DateTime dtCreate = reader.GetDateTime(4).ToLocalTime();
                        DateTime dtLastLogin = reader.GetDateTime(5).ToLocalTime();
                        DateTime dtLastActivity = reader.GetDateTime(6).ToLocalTime();
                        DateTime dtLastPassChange = reader.GetDateTime(7).ToLocalTime();
                        string? userName = GetNullableString(reader, 8);
                        bool isLockedOut = reader.GetBoolean(9);
                        DateTime dtLastLockoutDate = reader.GetDateTime(10).ToLocalTime();

                        return new MembershipUser(
                            providerName: _settings.ApplicationName,
                            userName: userName,
                            providerUserKey: providerUserKey,
                            email: email,
                            passwordQuestion: passwordQuestion,
                            comment: comment,
                            isApproved: isApproved,
                            isLockedOut: isLockedOut,
                            creationDate: dtCreate,
                            lastLoginDate: dtLastLogin,
                            lastActivityDate: dtLastActivity,
                            lastPasswordChangedDate: dtLastPassChange,
                            lastLockoutDate: dtLastLockoutDate);
                    }

                    return null;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task ResetPassword(string username, string newPasswordEncoded, string? passwordSalt, string? passwordAnswer, int passwordFormat)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_ResetPassword", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, newPasswordEncoded));
                    cmd.Parameters.Add(CreateInputParam("@MaxInvalidPasswordAttempts", SqlDbType.Int, _settings.MaxInvalidPasswordAttempts));
                    cmd.Parameters.Add(CreateInputParam("@PasswordAttemptWindow", SqlDbType.Int, _settings.PasswordAttemptWindow));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, passwordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    if (_settings.RequiresQuestionAndAnswer)
                    {
                        cmd.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, passwordAnswer));
                    }

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    await cmd.ExecuteNonQueryAsync();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        string errText = GetExceptionText(status);

                        if (IsStatusDueToBadPassword(status))
                        {
                            throw new MembershipPasswordException(errText);
                        }
                        else
                        {
                            throw new ProviderException(errText);
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<GetPasswordWithFormatResponse> GetPasswordWithFormat(string username, bool updateLastLoginActivityDate)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };

                try
                {
                    await connection.OpenAsync();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetPasswordWithFormat", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastLoginActivityDate", SqlDbType.Bit, updateLastLoginActivityDate));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));
                    cmd.Parameters.Add(p);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);

                    if (await reader.ReadAsync())
                    {
                        string password = reader.GetString(0);
                        int passwordFormat = reader.GetInt32(1);
                        string passwordSalt = reader.GetString(2);
                        int failedPasswordAttemptCount = reader.GetInt32(3);
                        int failedPasswordAnswerAttemptCount = reader.GetInt32(4);
                        bool isApproved = reader.GetBoolean(5);
                        DateTime lastLoginDate = reader.GetDateTime(6);
                        DateTime lastActivityDate = reader.GetDateTime(7);

                        return new GetPasswordWithFormatResponse()
                        {
                            Password = password,
                            PasswordFormat = passwordFormat,
                            PasswordSalt = passwordSalt,
                            FailedPasswordAttemptCount = failedPasswordAttemptCount,
                            FailedPasswordAnswerAttemptCount = failedPasswordAnswerAttemptCount,
                            IsApproved = isApproved,
                            LastLoginDate = lastLoginDate,
                            LastActivityDate = lastActivityDate,
                            Status = (p?.Value != null) ? ((int)p.Value) : -1
                        };
                    }
                    else
                    {
                        return new GetPasswordWithFormatResponse()
                        {
                            Password = null,
                            PasswordFormat = 0,
                            PasswordSalt = null,
                            FailedPasswordAttemptCount = 0,
                            FailedPasswordAnswerAttemptCount = 0,
                            IsApproved = false,
                            LastLoginDate = DateTime.UtcNow,
                            LastActivityDate = DateTime.UtcNow,
                            Status = (p?.Value != null) ? ((int)p.Value) : -1
                        };
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task CheckPassword(string username, bool isPasswordCorrect, bool updateLastLoginActivityDate, DateTime lastLoginDate, DateTime lastActivityDate)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    DateTime dtNow = DateTime.UtcNow;

                    SqlCommand cmd = new("dbo.aspnet_Membership_UpdateUserInfo", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@IsPasswordCorrect", SqlDbType.Bit, isPasswordCorrect));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastLoginActivityDate", SqlDbType.Bit, updateLastLoginActivityDate));
                    cmd.Parameters.Add(CreateInputParam("@MaxInvalidPasswordAttempts", SqlDbType.Int, _settings.MaxInvalidPasswordAttempts));
                    cmd.Parameters.Add(CreateInputParam("@PasswordAttemptWindow", SqlDbType.Int, _settings.PasswordAttemptWindow));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, dtNow));
                    cmd.Parameters.Add(CreateInputParam("@LastLoginDate", SqlDbType.DateTime, isPasswordCorrect ? dtNow : lastLoginDate));
                    cmd.Parameters.Add(CreateInputParam("@LastActivityDate", SqlDbType.DateTime, isPasswordCorrect ? dtNow : lastActivityDate));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);
                    await cmd.ExecuteNonQueryAsync();
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                throw;
            }
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

        private static string? GetNullableString(SqlDataReader reader, int col) =>
            reader.IsDBNull(col) ? null : reader.GetString(col);

        internal static bool IsStatusDueToBadPassword(int status) =>
            status >= 2 && status <= 6 || status == 99;

        private static DateTime RoundToSeconds(DateTime utcDateTime) =>
            new(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day, utcDateTime.Hour, utcDateTime.Minute, utcDateTime.Second, DateTimeKind.Utc);

        internal static string GetExceptionText(int status) => status switch
        {
            0 => string.Empty,
            1 => "The user was not found.",
            2 => "The password supplied is wrong.",
            3 => "The password-answer supplied is wrong.",
            4 => "The password supplied is invalid.  Passwords must conform to the password strength requirements configured for the default provider.",
            5 => "The password-question supplied is invalid.  Note that the current provider configuration requires a valid password question and answer.  As a result, a CreateUser overload that accepts question and answer parameters must also be used.",
            6 => "The password-answer supplied is invalid.",
            7 => "The E-mail supplied is invalid.",
            99 => "The user account has been locked out.",
            _ => "The Provider encountered an unknown error.",
        };
    }
}