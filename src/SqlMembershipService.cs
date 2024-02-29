using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlMembershipAdapter
{
    public class SqlMembershipService
    {
        private const int SALT_SIZE = 16;
        private const int PASSWORD_SIZE = 14;

        private readonly SqlMembershipSettings _settings;

        public SqlMembershipService(SqlMembershipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public MembershipUser CreateUser(
            string username,
            string password,
            string email,
            string passwordQuestion,
            string passwordAnswer,
            bool isApproved,
            object providerUserKey,
            out MembershipCreateStatus status)
        {
            if (!SecUtility.ValidateParameter(ref password, true, true, false, 128))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            string salt = GenerateSalt();
            string pass = EncodePassword(password, (int)_settings.PasswordFormat, salt);

            if (pass.Length > 128)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            string encodedPasswordAnswer;

            if (passwordAnswer != null)
            {
                passwordAnswer = passwordAnswer.Trim();
            }

            if (!string.IsNullOrEmpty(passwordAnswer))
            {
                if (passwordAnswer.Length > 128)
                {
                    status = MembershipCreateStatus.InvalidAnswer;
                    return null;
                }
                encodedPasswordAnswer = EncodePassword(passwordAnswer.ToLower(CultureInfo.InvariantCulture), (int)_settings.PasswordFormat, salt);
            }
            else
            {
                encodedPasswordAnswer = passwordAnswer;
            }

            if (!SecUtility.ValidateParameter(ref encodedPasswordAnswer, _settings.RequiresQuestionAndAnswer, true, false, 128))
            {
                status = MembershipCreateStatus.InvalidAnswer;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref username, true, true, true, 256))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref email, _settings.RequiresUniqueEmail, _settings.RequiresUniqueEmail, false, 256))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref passwordQuestion, _settings.RequiresQuestionAndAnswer, true, false, 256))
            {
                status = MembershipCreateStatus.InvalidQuestion;
                return null;
            }

            if (providerUserKey != null)
            {
                if (providerUserKey is not Guid)
                {
                    status = MembershipCreateStatus.InvalidProviderUserKey;
                    return null;
                }
            }

            if (password.Length < _settings.MinRequiredPasswordLength)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            int count = 0;

            for (int i = 0; i < password.Length; i++)
            {
                if (!char.IsLetterOrDigit(password, i))
                {
                    count++;
                }
            }

            if (count < _settings.MinRequiredNonAlphanumericCharacters)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (_settings.PasswordStrengthRegularExpression.Length > 0)
            {
                if (!IsMatch(password, _settings.PasswordStrengthRegularExpression, RegexOptions.None, _settings.PasswordStrengthRegexTimeout))
                {
                    status = MembershipCreateStatus.InvalidPassword;
                    return null;
                }
            }

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

                    DateTime dt = RoundToSeconds(DateTime.UtcNow);
                    SqlCommand cmd = new("dbo.aspnet_Membership_CreateUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@Password", SqlDbType.NVarChar, pass));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, salt));
                    cmd.Parameters.Add(CreateInputParam("@Email", SqlDbType.NVarChar, email));
                    cmd.Parameters.Add(CreateInputParam("@PasswordQuestion", SqlDbType.NVarChar, passwordQuestion));
                    cmd.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, encodedPasswordAnswer));
                    cmd.Parameters.Add(CreateInputParam("@IsApproved", SqlDbType.Bit, isApproved));
                    cmd.Parameters.Add(CreateInputParam("@UniqueEmail", SqlDbType.Int, _settings.RequiresUniqueEmail ? 1 : 0));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, (int)_settings.PasswordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, dt));

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
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException sqlEx)
                    {
                        if (sqlEx.Number == 2627 || sqlEx.Number == 2601 || sqlEx.Number == 2512)
                        {
                            status = MembershipCreateStatus.DuplicateUserName;
                            return null;
                        }

                        throw;
                    }

                    int iStatus = (p.Value != null) ? ((int)p.Value) : -1;

                    if (iStatus < 0 || iStatus > (int)MembershipCreateStatus.ProviderError)
                    {
                        iStatus = (int)MembershipCreateStatus.ProviderError;
                    }

                    status = (MembershipCreateStatus)iStatus;

                    if (iStatus != 0)
                    {
                        return null;
                    }

                    providerUserKey = new Guid(cmd.Parameters["@UserId"].Value.ToString());

                    dt = dt.ToLocalTime();

                    return new MembershipUser(
                        providerName: _settings.ApplicationName,
                        userName: username,
                        providerUserKey: providerUserKey,
                        email: email,
                        passwordQuestion: passwordQuestion,
                        comment: null,
                        isApproved: isApproved,
                        isLockedOut: false,
                        creationDate: dt,
                        lastLoginDate: dt,
                        lastActivityDate: dt,
                        lastPasswordChangedDate: dt,
                        lastLockoutDate: new DateTime(1754, 1, 1));
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        private static bool IsMatch(string stringToMatch, string pattern, RegexOptions regOption, int? timeoutInMillsec)
        {
            if (timeoutInMillsec.HasValue && timeoutInMillsec.Value > 0)
            {
                return Regex.IsMatch(stringToMatch, pattern, regOption, TimeSpan.FromMilliseconds(timeoutInMillsec.Value));
            }
            else
            {
                return Regex.IsMatch(stringToMatch, pattern, regOption);
            }
        }

        public bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            SecUtility.CheckParameter(ref username, true, true, true, 256, "username");
            SecUtility.CheckParameter(ref password, true, true, false, 128, "password");

            string salt;
            int passwordFormat;

            if (!CheckPassword(username, password, false, false, out salt, out passwordFormat))
            {
                return false;
            }

            SecUtility.CheckParameter(ref newPasswordQuestion, _settings.RequiresQuestionAndAnswer, _settings.RequiresQuestionAndAnswer, false, 256, "newPasswordQuestion");
            string encodedPasswordAnswer;
            if (newPasswordAnswer != null)
            {
                newPasswordAnswer = newPasswordAnswer.Trim();
            }

            // VSWhidbey 421267: We need to check the length before we encode as well as after
            SecUtility.CheckParameter(ref newPasswordAnswer, _settings.RequiresQuestionAndAnswer, _settings.RequiresQuestionAndAnswer, false, 128, "newPasswordAnswer");
            if (!string.IsNullOrEmpty(newPasswordAnswer))
            {
                encodedPasswordAnswer = EncodePassword(newPasswordAnswer.ToLower(CultureInfo.InvariantCulture), (int)passwordFormat, salt);
            }
            else
            {
                encodedPasswordAnswer = newPasswordAnswer;
            }

            SecUtility.CheckParameter(ref encodedPasswordAnswer, _settings.RequiresQuestionAndAnswer, _settings.RequiresQuestionAndAnswer, false, 128, "newPasswordAnswer");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_ChangePasswordQuestionAndAnswer", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPasswordQuestion", SqlDbType.NVarChar, newPasswordQuestion));
                    cmd.Parameters.Add(CreateInputParam("@NewPasswordAnswer", SqlDbType.NVarChar, encodedPasswordAnswer));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        throw new ProviderException(GetExceptionText(status));
                    }

                    return status == 0;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            SecUtility.CheckParameter(ref username, true, true, true, 256, "username");
            SecUtility.CheckParameter(ref oldPassword, true, true, false, 128, "oldPassword");
            SecUtility.CheckParameter(ref newPassword, true, true, false, 128, "newPassword");

            string salt = null;
            int passwordFormat;
            int status;

            if (!CheckPassword(username, oldPassword, false, false, out salt, out passwordFormat))
            {
                return false;
            }

            if (newPassword.Length < _settings.MinRequiredPasswordLength)
            {
                throw new ArgumentException(
                    string.Format("The length of parameter '{0}' needs to be greater or equal to '{1}'.",
                        "newPassword", _settings.MinRequiredPasswordLength.ToString(CultureInfo.InvariantCulture)));
            }

            int count = 0;

            for (int i = 0; i < newPassword.Length; i++)
            {
                if (!char.IsLetterOrDigit(newPassword, i))
                {
                    count++;
                }
            }

            if (count < _settings.MinRequiredNonAlphanumericCharacters)
            {
                throw new ArgumentException(
                    string.Format("Non alpha numeric characters in '{0}' needs to be greater than or equal to '{1}'.",
                        "newPassword", _settings.MinRequiredNonAlphanumericCharacters.ToString(CultureInfo.InvariantCulture)));
            }

            if (_settings.PasswordStrengthRegularExpression.Length > 0)
            {
                if (!IsMatch(newPassword, _settings.PasswordStrengthRegularExpression, RegexOptions.None, _settings.PasswordStrengthRegexTimeout))
                {
                    throw new ArgumentException(
                        string.Format("The parameter '{0}' does not match the regular expression specified in config file.", "newPassword"));
                }
            }

            string pass = EncodePassword(newPassword, passwordFormat, salt);

            if (pass.Length > 128)
            {
                throw new ArgumentException("The password is too long: it must not exceed 128 chars after encrypting.", "newPassword");
            }

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_SetPassword", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, pass));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, salt));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, passwordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public string ResetPassword(string username, string passwordAnswer)
        {
            if (!_settings.EnablePasswordReset)
            {
                throw new NotSupportedException("Password resets not configured: set EnablePasswordReset to \"true\" in the configuration file.");
            }

            SecUtility.CheckParameter(ref username, true, true, true, 256, "username");

            string salt;
            int passwordFormat;
            string passwdFromDB;
            int status;
            int failedPasswordAttemptCount;
            int failedPasswordAnswerAttemptCount;
            bool isApproved;
            DateTime lastLoginDate, lastActivityDate;

            GetPasswordWithFormat(username, false, out status, out passwdFromDB, out passwordFormat, out salt, out failedPasswordAttemptCount,
                                  out failedPasswordAnswerAttemptCount, out isApproved, out lastLoginDate, out lastActivityDate);
            if (status != 0)
            {
                if (IsStatusDueToBadPassword(status))
                {
                    throw new MembershipPasswordException(GetExceptionText(status));
                }
                else
                {
                    throw new ProviderException(GetExceptionText(status));
                }
            }

            string encodedPasswordAnswer;
            if (passwordAnswer != null)
            {
                passwordAnswer = passwordAnswer.Trim();
            }
            if (!string.IsNullOrEmpty(passwordAnswer))
            {
                encodedPasswordAnswer = EncodePassword(passwordAnswer.ToLower(CultureInfo.InvariantCulture), passwordFormat, salt);
            }
            else
            {
                encodedPasswordAnswer = passwordAnswer;
            }

            SecUtility.CheckParameter(ref encodedPasswordAnswer, _settings.RequiresQuestionAndAnswer, _settings.RequiresQuestionAndAnswer, false, 128, "passwordAnswer");
            string newPassword = GeneratePassword();

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    connection.Open();

                    string errText;

                    SqlCommand cmd = new("dbo.aspnet_Membership_ResetPassword", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@NewPassword", SqlDbType.NVarChar, EncodePassword(newPassword, (int)passwordFormat, salt)));
                    cmd.Parameters.Add(CreateInputParam("@MaxInvalidPasswordAttempts", SqlDbType.Int, _settings.MaxInvalidPasswordAttempts));
                    cmd.Parameters.Add(CreateInputParam("@PasswordAttemptWindow", SqlDbType.Int, _settings.PasswordAttemptWindow));
                    cmd.Parameters.Add(CreateInputParam("@PasswordSalt", SqlDbType.NVarChar, salt));
                    cmd.Parameters.Add(CreateInputParam("@PasswordFormat", SqlDbType.Int, (int)passwordFormat));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));
                    
                    if (_settings.RequiresQuestionAndAnswer)
                    {
                        cmd.Parameters.Add(CreateInputParam("@PasswordAnswer", SqlDbType.NVarChar, encodedPasswordAnswer));
                    }

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

                    status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        errText = GetExceptionText(status);

                        if (IsStatusDueToBadPassword(status))
                        {
                            throw new MembershipPasswordException(errText);
                        }
                        else
                        {
                            throw new ProviderException(errText);
                        }
                    }

                    return newPassword;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public void UpdateUser(MembershipUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            string temp = user.UserName;
            SecUtility.CheckParameter(ref temp, true, true, true, 256, "UserName");
            temp = user.Email;

            SecUtility.CheckParameter(ref temp, _settings.RequiresUniqueEmail, _settings.RequiresUniqueEmail, false, 256, "Email");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

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
                    cmd.ExecuteNonQuery();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    if (status != 0)
                    {
                        throw new ProviderException(GetExceptionText(status));
                    }

                    return;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public bool ValidateUser(string username, string password)
        {
            return
                SecUtility.ValidateParameter(ref username, true, true, true, 256) &&
                SecUtility.ValidateParameter(ref password, true, true, false, 128) &&
                CheckPassword(username, password, true, true);
        }

        public bool UnlockUser(string username)
        {
            SecUtility.CheckParameter(ref username, true, true, true, 256, "username");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_UnlockUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;
                    if (status == 0)
                    {
                        return true;
                    }

                    return false;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (providerUserKey == null)
            {
                throw new ArgumentNullException("providerUserKey");
            }

            if (providerUserKey is not Guid)
            {
                throw new ArgumentException("The provider user key supplied is invalid.  It must be of type System.Guid.", "providerUserKey");
            }

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

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

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string email = GetNullableString(reader, 0);
                        string passwordQuestion = GetNullableString(reader, 1);
                        string comment = GetNullableString(reader, 2);
                        bool isApproved = reader.GetBoolean(3);
                        DateTime dtCreate = reader.GetDateTime(4).ToLocalTime();
                        DateTime dtLastLogin = reader.GetDateTime(5).ToLocalTime();
                        DateTime dtLastActivity = reader.GetDateTime(6).ToLocalTime();
                        DateTime dtLastPassChange = reader.GetDateTime(7).ToLocalTime();
                        string userName = GetNullableString(reader, 8);
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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public MembershipUser GetUser(string username, bool userIsOnline)
        {
            SecUtility.CheckParameter(ref username, true, false, true, 256, "username");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetUserByName", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastActivity", SqlDbType.Bit, userIsOnline));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string email = GetNullableString(reader, 0);
                        string passwordQuestion = GetNullableString(reader, 1);
                        string comment = GetNullableString(reader, 2);
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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public string GetUserNameByEmail(string email)
        {
            SecUtility.CheckParameter(ref email, false, false, false, 256, "email");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

                    string username = null;

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

                    using SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

                    if (reader.Read())
                    {
                        username = GetNullableString(reader, 0);

                        if (_settings.RequiresUniqueEmail && reader.Read())
                        {
                            throw new ProviderException("More than one user has the specified e-mail address.");
                        }
                    }

                    return username;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            SecUtility.CheckParameter(ref username, true, true, true, 256, "username");

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                try
                {
                    connection.Open();
                    SqlCommand cmd = new("dbo.aspnet_Users_DeleteUser", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));

                    if (deleteAllRelatedData)
                    {
                        cmd.Parameters.Add(CreateInputParam("@TablesToDeleteFrom", SqlDbType.Int, 0xF));
                    }
                    else
                    {
                        cmd.Parameters.Add(CreateInputParam("@TablesToDeleteFrom", SqlDbType.Int, 1));
                    }

                    SqlParameter p = new("@NumTablesDeletedFrom", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    cmd.Parameters.Add(p);
                    cmd.ExecuteNonQuery();

                    int status = (p.Value != null) ? ((int)p.Value) : -1;

                    return status > 0;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public List<MembershipUser> GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("The pageIndex must be greater than or equal to zero.", "pageIndex");
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("The pageSize must be greater than zero.", "pageSize");
            }

            long upperBound = (long)pageIndex * pageSize + pageSize - 1;

            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("The combination of pageIndex and pageSize cannot exceed the maximum value of System.Int32.", "pageIndex and pageSize");
            }

            List<MembershipUser> users = [];

            totalRecords = 0;
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

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
                        using SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

                        while (reader.Read())
                        {
                            string username, email, passwordQuestion, comment;
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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }

            return users;
        }

        public int GetNumberOfUsersOnline(int timeWindowMinutes)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                try
                {
                    connection.Open();

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
                    cmd.ExecuteNonQuery();
                    int num = (p.Value != null) ? ((int)p.Value) : -1;
                    return num;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public List<MembershipUser> FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            SecUtility.CheckParameter(ref usernameToMatch, true, true, false, 256, "usernameToMatch");

            if (pageIndex < 0)
            {
                throw new ArgumentException("The pageIndex must be greater than or equal to zero.", "pageIndex");
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("The pageSize must be greater than zero.", "pageSize");
            }

            long upperBound = (long)pageIndex * pageSize + pageSize - 1;

            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("The combination of pageIndex and pageSize cannot exceed the maximum value of System.Int32.", "pageIndex and pageSize");
            }

            try
            {
                List<MembershipUser> users = [];

                SqlConnection connection = new(_settings.ConnectionString);

                totalRecords = 0;
                SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };

                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_FindUsersByName", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserNameToMatch", SqlDbType.NVarChar, usernameToMatch));
                    cmd.Parameters.Add(CreateInputParam("@PageIndex", SqlDbType.Int, pageIndex));
                    cmd.Parameters.Add(CreateInputParam("@PageSize", SqlDbType.Int, pageSize));
                    cmd.Parameters.Add(p);

                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                            while (reader.Read())
                            {
                                string username, email, passwordQuestion, comment;
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

                        return users;
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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        public List<MembershipUser> FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            SecUtility.CheckParameter(ref emailToMatch, false, false, false, 256, "emailToMatch");

            if (pageIndex < 0)
            {
                throw new ArgumentException("The pageIndex must be greater than or equal to zero.", "pageIndex");
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("The pageSize must be greater than zero.", "pageSize");
            }

            long upperBound = (long)pageIndex * pageSize + pageSize - 1;

            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("The combination of pageIndex and pageSize cannot exceed the maximum value of System.Int32.", "pageIndex and pageSize");
            }

            try
            {
                SqlConnection connection = new(_settings.ConnectionString);

                totalRecords = 0;

                SqlParameter p = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };

                try
                {
                    connection.Open();

                    List<MembershipUser> users = [];

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
                        using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                            while (reader.Read())
                            {
                                string username, email, passwordQuestion, comment;
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

                        return users;
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
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }
        }

        private bool CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved)
        {
            string salt;
            int passwordFormat;
            return CheckPassword(username, password, updateLastLoginActivityDate, failIfNotApproved, out salt, out passwordFormat);
        }

        private bool CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved, out string salt, out int passwordFormat)
        {
            SqlConnection connection = new(_settings.ConnectionString);

            string passwdFromDB;
            int status;
            int failedPasswordAttemptCount;
            int failedPasswordAnswerAttemptCount;
            bool isPasswordCorrect;
            bool isApproved;
            DateTime lastLoginDate, lastActivityDate;

            GetPasswordWithFormat(username, updateLastLoginActivityDate, out status, out passwdFromDB, out passwordFormat, out salt, out failedPasswordAttemptCount,
                                  out failedPasswordAnswerAttemptCount, out isApproved, out lastLoginDate, out lastActivityDate);
            if (status != 0)
            {
                return false;
            }

            if (!isApproved && failIfNotApproved)
            {
                return false;
            }

            string encodedPasswd = EncodePassword(password, passwordFormat, salt);

            isPasswordCorrect = passwdFromDB.Equals(encodedPasswd);

            if (isPasswordCorrect && failedPasswordAttemptCount == 0 && failedPasswordAnswerAttemptCount == 0)
            {
                return true;
            }

            try
            {
                try
                {
                    connection.Open();

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

                    cmd.ExecuteNonQuery();

                    status = (p.Value != null) ? ((int)p.Value) : -1;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch
            {
                throw;
            }

            return isPasswordCorrect;
        }

        private void GetPasswordWithFormat(
            string username,
            bool updateLastLoginActivityDate,
            out int status,
            out string password,
            out int passwordFormat,
            out string passwordSalt,
            out int failedPasswordAttemptCount,
            out int failedPasswordAnswerAttemptCount,
            out bool isApproved,
            out DateTime lastLoginDate,
            out DateTime lastActivityDate)
        {
            try
            {
                SqlConnection connection = new(_settings.ConnectionString);
                SqlParameter p = null;

                try
                {
                    connection.Open();

                    SqlCommand cmd = new("dbo.aspnet_Membership_GetPasswordWithFormat", connection)
                    {
                        CommandTimeout = _settings.CommandTimeoutSeconds,
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(CreateInputParam("@ApplicationName", SqlDbType.NVarChar, _settings.ApplicationName));
                    cmd.Parameters.Add(CreateInputParam("@UserName", SqlDbType.NVarChar, username));
                    cmd.Parameters.Add(CreateInputParam("@UpdateLastLoginActivityDate", SqlDbType.Bit, updateLastLoginActivityDate));
                    cmd.Parameters.Add(CreateInputParam("@CurrentTimeUtc", SqlDbType.DateTime, DateTime.UtcNow));

                    p = new SqlParameter("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };

                    cmd.Parameters.Add(p);

                    using SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                    status = -1;

                    if (reader.Read())
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
                    else
                    {
                        password = null;
                        passwordFormat = 0;
                        passwordSalt = null;
                        failedPasswordAttemptCount = 0;
                        failedPasswordAnswerAttemptCount = 0;
                        isApproved = false;
                        lastLoginDate = DateTime.UtcNow;
                        lastActivityDate = DateTime.UtcNow;
                    }
                }
                finally
                {
                    status = (p?.Value != null) ? ((int)p.Value) : -1;

                    connection.Close();
                }
            }
            catch
            {
                throw;
            }

        }

        private static char[] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        public string GeneratePassword()
        {
            int length = _settings.MinRequiredPasswordLength < PASSWORD_SIZE ? PASSWORD_SIZE : _settings.MinRequiredPasswordLength;
            int numberOfNonAlphanumericCharacters = _settings.MinRequiredNonAlphanumericCharacters;

            if (length < 1 || length > 128)
            {
                throw new ArgumentException("Password length specified must be between 1 and 128 characters.");
            }

            if (numberOfNonAlphanumericCharacters > length || numberOfNonAlphanumericCharacters < 0)
            {
                throw new ArgumentException(
                    string.Format("The value specified in parameter '{0}' should be in the range from zero to the value specified in the password length parameter.", "numberOfNonAlphanumericCharacters"));
            }

            string password;
            byte[] buf;
            char[] cBuf;
            int count;

            do
            {
                buf = new byte[length];
                cBuf = new char[length];
                count = 0;

                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(buf);
                }

                for (int iter = 0; iter < length; iter++)
                {
                    int i = (int)(buf[iter] % 87);
                    if (i < 10)
                        cBuf[iter] = (char)('0' + i);
                    else if (i < 36)
                        cBuf[iter] = (char)('A' + i - 10);
                    else if (i < 62)
                        cBuf[iter] = (char)('a' + i - 36);
                    else
                    {
                        cBuf[iter] = punctuations[i - 62];
                        count++;
                    }
                }

                if (count < numberOfNonAlphanumericCharacters)
                {
                    int j, k;
                    Random rand = new Random();

                    for (j = 0; j < numberOfNonAlphanumericCharacters - count; j++)
                    {
                        do
                        {
                            k = rand.Next(0, length);
                        }
                        while (!Char.IsLetterOrDigit(cBuf[k]));

                        cBuf[k] = punctuations[rand.Next(0, punctuations.Length)];
                    }
                }

                password = new string(cBuf);
            }
            while (IsDangerousString(password));

            return password;
        }

        private static readonly char[] startingChars = ['<', '&'];

        internal static bool IsDangerousString(string s)
        {
            for (int i = 0; ;)
            {
                int n = s.IndexOfAny(startingChars, i);

                if (n < 0) return false;

                if (n == s.Length - 1) return false;

                switch (s[n])
                {
                    case '<':
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?') return true;
                        break;
                    case '&':
                        if (s[n + 1] == '#') return true;
                        break;
                }

                i = n + 1;
            }
        }

        private static bool IsAtoZ(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private SqlParameter CreateInputParam(string paramName, SqlDbType dbType, object objValue)
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

        private string? GetNullableString(SqlDataReader reader, int col)
        {
            if (reader.IsDBNull(col) == false)
            {
                return reader.GetString(col);
            }

            return null;
        }

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

        internal static bool IsStatusDueToBadPassword(int status) =>
            status >= 2 && status <= 6 || status == 99;

        private static DateTime RoundToSeconds(DateTime utcDateTime) =>
            new(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day, utcDateTime.Hour, utcDateTime.Minute, utcDateTime.Second, DateTimeKind.Utc);

        private string EncodePassword(string pass, int passwordFormat, string salt)
        {
            if (passwordFormat != 1)
            {
                return pass;
            }

            byte[] bIn = Encoding.Unicode.GetBytes(pass);
            byte[] bSalt = Convert.FromBase64String(salt);
            byte[] bRet = null;

            HashAlgorithm hm = _settings.HashAlgorithm switch
            {
                HashAlgorithmType.SHA1 => SHA1.Create(),
                _ => MD5.Create(),
            };

            if (hm is KeyedHashAlgorithm)
            {
                KeyedHashAlgorithm kha = (KeyedHashAlgorithm)hm;

                if (kha.Key.Length == bSalt.Length)
                {
                    kha.Key = bSalt;
                }
                else if (kha.Key.Length < bSalt.Length)
                {
                    byte[] bKey = new byte[kha.Key.Length];
                    Buffer.BlockCopy(bSalt, 0, bKey, 0, bKey.Length);
                    kha.Key = bKey;
                }
                else
                {
                    byte[] bKey = new byte[kha.Key.Length];
                    for (int iter = 0; iter < bKey.Length;)
                    {
                        int len = Math.Min(bSalt.Length, bKey.Length - iter);
                        Buffer.BlockCopy(bSalt, 0, bKey, iter, len);
                        iter += len;
                    }
                    kha.Key = bKey;
                }
                bRet = kha.ComputeHash(bIn);
            }
            else
            {
                byte[] bAll = new byte[bSalt.Length + bIn.Length];
                Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
                Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);
                bRet = hm.ComputeHash(bAll);
            }

            return Convert.ToBase64String(bRet);
        }

        private string GenerateSalt()
        {
            byte[] buf = new byte[SALT_SIZE];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buf);
            }
            return Convert.ToBase64String(buf);
        }
    }
}