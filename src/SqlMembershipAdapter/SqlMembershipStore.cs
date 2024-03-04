using System.Data;
using System.Data.SqlClient;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Exceptions;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Result;

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
            DateTime currentTime = RoundToSeconds(DateTime.UtcNow);

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_CreateUser", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, userName));
            command.Parameters.Add(CreateInputParam("@Password", SqlDbType.NVarChar, password));
            command.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
            command.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, email));
            command.Parameters.Add(CreateInputParam("@PasswordQuestion", SqlDbType.NVarChar, passwordQuestion));
            command.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, passwordAnswer));
            command.Parameters.Add(CreateInputParam("@IsApproved", SqlDbType.Bit, isApproved));
            command.Parameters.Add(CreateInputParam("@UniqueEmail", SqlDbType.Int, _settings.RequiresUniqueEmail ? 1 : 0));
            command.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, (int)_settings.PasswordFormat));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, currentTime));

            SqlParameter userIdParam = command.Parameters.Add(CreateInputParam("@UserId", SqlDbType.UniqueIdentifier, providerUserKey));
            userIdParam.Direction = ParameterDirection.InputOutput;

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601 || sqlEx.Number == 2512)
                {
                    return new CreateUserResult(MembershipCreateStatus.DuplicateUserName);
                }

                throw;
            }

            int iStatus = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            if (iStatus < 0 || iStatus > (int)MembershipCreateStatus.ProviderError)
            {
                iStatus = (int)MembershipCreateStatus.ProviderError;
            }

            MembershipCreateStatus status = (MembershipCreateStatus)iStatus;

            if (iStatus != 0)
            {
                return new CreateUserResult(status);
            }

            string? userIdText = userIdParam.Value?.ToString();

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

        public async Task ChangePasswordQuestionAndAnswer(string username, string password, string? newPasswordQuestion, string? newPasswordAnswer)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_ChangePasswordQuestionAndAnswer", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@NewPasswordQuestion", SqlDbType.NVarChar, newPasswordQuestion));
            command.Parameters.Add(CreateInputParam("@NewPasswordAnswer", SqlDbType.NVarChar, newPasswordAnswer));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            if (status != 0)
            {
                throw new ProviderException(GetExceptionText(status));
            }
        }

        public async Task<MembershipUserCollection> FindUsersByName(string usernameToMatch, int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_FindUsersByName", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserNameToMatch", SqlDbType.NVarChar, usernameToMatch));
            command.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
            command.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

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

            if (returnParameter?.Value is not null and int)
            {
                totalRecords = (int)returnParameter.Value;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<MembershipUserCollection> FindUsersByEmail(string? emailToMatch, int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_FindUsersByEmail", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@EmailToMatch", SqlDbType.NVarChar, emailToMatch));
            command.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
            command.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

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

            if (returnParameter?.Value is not null and int)
            {
                totalRecords = (int)returnParameter.Value;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<bool> ChangePassword(string username, string newPassword, string? passwordSalt, int passwordFormat)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_SetPassword", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, newPassword));
            command.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
            command.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, passwordFormat));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

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

        public async Task UpdateUser(MembershipUser user)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_UpdateUser", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, user.UserName));
            command.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, user.Email));
            command.Parameters.Add(CreateInputParam("@Comment", SqlDbType.NText, user.Comment));
            command.Parameters.Add(CreateInputParam("@IsApproved", SqlDbType.Bit, user.IsApproved ? 1 : 0));
            command.Parameters.Add(CreateInputParam("@LastLoginDate", SqlDbType.DateTime, user.LastLoginDate.ToUniversalTime()));
            command.Parameters.Add(CreateInputParam("@LastActivityDate", SqlDbType.DateTime, user.LastActivityDate.ToUniversalTime()));
            command.Parameters.Add(CreateInputParam("@UniqueEmail", SqlDbType.Int, _settings.RequiresUniqueEmail ? 1 : 0));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            if (status != 0)
            {
                throw new ProviderException(GetExceptionText(status));
            }
        }

        public async Task<bool> UnlockUser(string userName)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_UnlockUser", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, userName));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            return status == 0;
        }

        public async Task<MembershipUser?> GetUser(string username, bool updateLastActivity)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetUserByName", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@UpdateLastActivity", SqlDbType.Bit, updateLastActivity));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

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

        public async Task<int> GetNumberOfUsersOnline(int timeWindowMinutes)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetNumberOfUsersOnline", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@MinutesSinceLastInActive", SqlDbType.Int, timeWindowMinutes));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int onlineCount = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            return onlineCount;
        }

        public async Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize)
        {
            List<MembershipUser> users = [];
            int totalRecords = 0;

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetAllUsers", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
            command.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

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

            if (returnParameter?.Value is not null and int)
            {
                totalRecords = (int)returnParameter.Value;
            }

            return new MembershipUserCollection(users, totalRecords);
        }

        public async Task<bool> DeleteUser(string username, bool deleteAllRelatedData)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Users_DeleteUser", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@TablesToDeleteFrom", SqlDbType.Int, deleteAllRelatedData ? 0xF : 1));

            SqlParameter returnParameter = command.Parameters.Add("@NumTablesDeletedFrom", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.Output;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            return status > 0;
        }

        public async Task<string?> GetUsernameByEmail(string? email)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetUserByEmail", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, email));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            string? username = null;

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

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

        public async Task<MembershipUser?> GetUser(Guid providerUserKey, bool userIsOnline)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetUserByUserId", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@UserId", SqlDbType.UniqueIdentifier, providerUserKey));
            command.Parameters.Add(CreateInputParam("@UpdateLastActivity", SqlDbType.Bit, userIsOnline));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

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

        public async Task ResetPassword(string username, string newPasswordEncoded, string? passwordSalt, string? passwordAnswer, int passwordFormat)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_ResetPassword", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, newPasswordEncoded));
            command.Parameters.Add(CreateInputParam("@MaxInvalidPasswordAttempts", SqlDbType.Int, _settings.MaxInvalidPasswordAttempts));
            command.Parameters.Add(CreateInputParam("@PasswordAttemptWindow", SqlDbType.Int, _settings.PasswordAttemptWindow));
            command.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, passwordSalt));
            command.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, passwordFormat));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            if (_settings.RequiresQuestionAndAnswer)
            {
                command.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, passwordAnswer));
            }

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

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

        public async Task<GetPasswordWithFormatResult> GetPasswordWithFormat(string username, bool updateLastLoginActivityDate)
        {
            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_GetPasswordWithFormat", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@UpdateLastLoginActivityDate", SqlDbType.Bit, updateLastLoginActivityDate));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

            int passwordFormat = 0;
            string? password = null;
            string? passwordSalt = null;
            int failedPasswordAttemptCount = 0;
            int failedPasswordAnswerAttemptCount = 0;
            bool isApproved = false;
            DateTime lastLoginDate = DateTime.UtcNow;
            DateTime lastActivityDate = DateTime.UtcNow;

            if (await reader.ReadAsync())
            {
                password = reader.GetString(0);
                passwordFormat = reader.GetInt32(1);
                passwordSalt = reader.GetString(2);
                failedPasswordAttemptCount = reader.GetInt32(3);
                failedPasswordAnswerAttemptCount = reader.GetInt32(4);
                isApproved = reader.GetBoolean(5);
                lastLoginDate = reader.GetDateTime(6);
                lastActivityDate = reader.GetDateTime(7);
            }

            int status = (returnParameter?.Value != null) ? ((int)returnParameter.Value) : -1;

            if (status != 0)
            {
                string exceptionText = GetExceptionText(status);

                throw (status is (>= 2 and <= 6) or 99)
                    ? new MembershipPasswordException(exceptionText) : new ProviderException(exceptionText);
            }

            return new GetPasswordWithFormatResult()
            {
                Password = password,
                PasswordFormat = passwordFormat,
                PasswordSalt = passwordSalt,
                FailedPasswordAttemptCount = failedPasswordAttemptCount,
                FailedPasswordAnswerAttemptCount = failedPasswordAnswerAttemptCount,
                IsApproved = isApproved,
                LastLoginDate = lastLoginDate,
                LastActivityDate = lastActivityDate
            };
        }

        public async Task CheckPassword(string username, bool isPasswordCorrect, bool updateLastLoginActivityDate, DateTime lastLoginDate, DateTime lastActivityDate)
        {
            DateTime utcNow = DateTime.UtcNow;

            using SqlConnection connection = new(_settings.ConnectionString);
            using SqlCommand command = new("dbo.aspnet_Membership_UpdateUserInfo", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = _settings.CommandTimeoutSeconds;

            command.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
            command.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
            command.Parameters.Add(CreateInputParam("@IsPasswordCorrect", SqlDbType.Bit, isPasswordCorrect));
            command.Parameters.Add(CreateInputParam("@UpdateLastLoginActivityDate", SqlDbType.Bit, updateLastLoginActivityDate));
            command.Parameters.Add(CreateInputParam("@MaxInvalidPasswordAttempts", SqlDbType.Int, _settings.MaxInvalidPasswordAttempts));
            command.Parameters.Add(CreateInputParam("@PasswordAttemptWindow", SqlDbType.Int, _settings.PasswordAttemptWindow));
            command.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, utcNow));
            command.Parameters.Add(CreateInputParam("@LastLoginDate", SqlDbType.DateTime, isPasswordCorrect ? utcNow : lastLoginDate));
            command.Parameters.Add(CreateInputParam("@LastActivityDate", SqlDbType.DateTime, isPasswordCorrect ? utcNow : lastActivityDate));

            SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
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

        private static bool IsStatusDueToBadPassword(int status) =>
            status >= 2 && status <= 6 || status == 99;

        private static DateTime RoundToSeconds(DateTime utcDateTime) =>
            new(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day, utcDateTime.Hour, utcDateTime.Minute, utcDateTime.Second, DateTimeKind.Utc);

        private static string GetExceptionText(int status) => status switch
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