using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Exceptions;
using SqlMembershipAdapter.Extensions;
using SqlMembershipAdapter.Implementation;
using SqlMembershipAdapter.Models;
using SqlMembershipAdapter.Models.Request;
using SqlMembershipAdapter.Models.Result;
using SqlMembershipAdapter.Store;

namespace SqlMembershipAdapter
{
    /// <summary>
    /// Adapter for SQL Membership calls.
    /// </summary>
    public class SqlMembershipClient : ISqlMembershipClient
    {
        private readonly ISqlMembershipStore _sqlStore;
        private readonly ISqlMembershipSettings _settings;
        private readonly ISqlMembershipValidator _validator;
        private readonly ISqlMembershipEncryption _encryption;

        /// <summary>
        /// Initialises a new SqlMembershipClient with the supplied SQL database connection string and default Membership settings.
        /// </summary>
        /// <param name="sqlConnectionString">The SQL Membership database connection string.</param>
        public SqlMembershipClient(
            string sqlConnectionString)
        {
            _settings = new SqlMembershipSettings(sqlConnectionString);
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipStore(_settings);
            _encryption = new SqlMembershipEncryption(_settings);
        }

        /// <summary>
        /// Initialises a new SqlMembershipClient with the supplied Membership settings.
        /// </summary>
        /// <param name="settings">Membership settings.</param>
        /// <exception cref="ArgumentNullException"/>
        public SqlMembershipClient(
            SqlMembershipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _encryption = new SqlMembershipEncryption(_settings);
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipStore(_settings);
        }

        /// <summary>
        /// Internal implementation for unit testing.
        /// </summary>
        internal SqlMembershipClient(
            ISqlMembershipStore sqlStore,
            ISqlMembershipValidator validator,
            ISqlMembershipEncryption encryption,
            ISqlMembershipSettings settings)
        {
            _settings = settings;
            _sqlStore = sqlStore;
            _validator = validator;
            _encryption = encryption;
        }

        /// <inheritdoc/>
        public async Task<CreateUserResult> CreateUser(CreateUserRequest request)
        {
            if (!_validator.ValidateCreateUserRequest(request, out MembershipCreateStatus status))
            {
                return new CreateUserResult(status);
            }

            string passwordSalt = _encryption.GenerateSalt();
            string? encodedPassword = _encryption.Encode(request.Password, (int)_settings.PasswordFormat, passwordSalt);

            if (!_validator.ValidatePassword(encodedPassword))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidPassword);
            }

            string? encodedPasswordAnswer = _encryption.Encode(request.PasswordAnswer, (int)_settings.PasswordFormat, passwordSalt);

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidAnswer);
            }

            CreateUserResult createResult = await _sqlStore.CreateUser(
                request.ProviderUserKey,
                request.Username,
                encodedPassword,
                passwordSalt,
                request.Email,
                request.PasswordQuestion,
                encodedPasswordAnswer,
                request.IsApproved).ConfigureAwait(false);

            return createResult;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangePasswordQuestionAndAnswer(ChangePasswordQuestionAndAnswerRequest request)
        {
            if (!_validator.ValidateChangePasswordQuestionAnswer(request, out string? invalidParam))
            {
                throw new ArgumentException(invalidParam);
            }

            CheckPasswordResult checkPassword = await CheckPassword(request.Username, request.Password, false, false);

            if (!checkPassword.IsValid)
            {
                return false;
            }

            string? encodedPasswordAnswer = _encryption.Encode(request.NewPasswordAnswer, checkPassword.PasswordFormat, checkPassword.PasswordSalt);

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                throw new ArgumentException(nameof(request.NewPasswordAnswer));
            }

            await _sqlStore.ChangePasswordQuestionAndAnswer(request.Username, request.Password, request.NewPasswordQuestion, encodedPasswordAnswer).ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangePassword(ChangePasswordRequest request)
        {
            if (!_validator.ValidateChangePasswordRequest(request, out string? invalidParam))
            {
                throw new ArgumentException(invalidParam);
            }

            CheckPasswordResult checkPassword = await CheckPassword(request.Username, request.OldPassword, false, false);

            if (!checkPassword.IsValid)
            {
                return false;
            }

            string? encodedPassword = _encryption.Encode(request.NewPassword, checkPassword.PasswordFormat, checkPassword.PasswordSalt) ?? string.Empty;

            if (!_validator.ValidatePassword(encodedPassword))
            {
                throw new ArgumentException(nameof(request.NewPassword));
            }

            bool isPasswordChanged = await _sqlStore.ChangePassword(request.Username, encodedPassword, checkPassword.PasswordSalt, checkPassword.PasswordFormat).ConfigureAwait(false);

            return isPasswordChanged;
        }

        /// <inheritdoc/>
        public async Task<PasswordData> GetPassword(string username)
        {
            GetPasswordWithFormatResult passwordWithFormat = await _sqlStore.GetPasswordWithFormat(username, false).ConfigureAwait(false);

            return new PasswordData()
            {
                Password = passwordWithFormat.Password,
                PasswordSalt = passwordWithFormat.PasswordSalt,
                PasswordFormatCode = passwordWithFormat.PasswordFormat
            };
        }

        /// <inheritdoc/>
        public async Task<string> ResetPassword(ResetPasswordRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException(nameof(request.Username));
            }

            GetPasswordWithFormatResult passwordWithFormat = await _sqlStore.GetPasswordWithFormat(request.Username, false);

            if (passwordWithFormat.StatusCode != 0)
            {
                string exceptionText = passwordWithFormat.StatusCode.ToProviderErrorText();

                throw (passwordWithFormat.StatusCode is (>= 2 and <= 6) or 99)
                    ? new MembershipPasswordException(exceptionText) : new ProviderException(exceptionText);
            }

            string? encodedPasswordAnswer = _encryption.Encode(request.PasswordAnswer, passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt);

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                throw new ArgumentException(nameof(request.PasswordAnswer));
            }

            string newPassword = await GeneratePassword();
            string newPasswordEncoded = _encryption.Encode(newPassword, passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt) ?? string.Empty;

            await _sqlStore.ResetPassword(request.Username, newPasswordEncoded, passwordWithFormat.PasswordSalt, encodedPasswordAnswer, passwordWithFormat.PasswordFormat).ConfigureAwait(false);

            return newPassword;
        }

        /// <inheritdoc/>
        public async Task UpdateUser(MembershipUser user)
        {
            if (!_validator.ValidateUsername(user.UserName))
            {
                throw new ArgumentException(nameof(user.UserName));
            }

            if (!_validator.ValidateEmail(user.Email))
            {
                throw new ArgumentException(nameof(user.Email));
            }

            await _sqlStore.UpdateUser(user).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateUser(ValidateUserRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                return false;
            }

            if (!_validator.ValidatePassword(request.Password))
            {
                return false;
            }

            CheckPasswordResult isValidPassword = await CheckPassword(request.Username, request.Password, true, true).ConfigureAwait(false);

            return isValidPassword.IsValid;
        }

        /// <inheritdoc/>
        public async Task<bool> UnlockUser(string username)
        {
            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            bool isUnlocked = await _sqlStore.UnlockUser(username).ConfigureAwait(false);

            return isUnlocked;
        }

        /// <inheritdoc/>
        public async Task<MembershipUser?> GetUser(string username, bool updateLastActivity)
        {
            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            MembershipUser? user = await _sqlStore.GetUser(username, updateLastActivity).ConfigureAwait(false);

            return user;
        }

        /// <inheritdoc/>
        public async Task<MembershipUser?> GetUser(Guid providerUserKey, bool updateLastActivity)
        {
            return await _sqlStore.GetUser(providerUserKey, updateLastActivity).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MembershipUser?> GetUser(object providerUserKey, bool updateLastActivity)
        {
            if (providerUserKey == null || !Guid.TryParse(providerUserKey.ToString(), out Guid userKey))
            {
                throw new ArgumentException(nameof(providerUserKey));
            }

            return await _sqlStore.GetUser(userKey, updateLastActivity).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string?> GetUserNameByEmail(string? email)
        {
            if (!_validator.ValidateEmail(email))
            {
                throw new ArgumentException(nameof(email));
            }

            string? userName = await _sqlStore.GetUsernameByEmail(email).ConfigureAwait(false);

            return userName;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUser(DeleteUserRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException(nameof(request.Username));
            }

            bool isDeleted = await _sqlStore.DeleteUser(request.Username, request.DeleteAllRelatedData).ConfigureAwait(false);

            return isDeleted;
        }

        /// <inheritdoc/>
        public async Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize)
        {
            if (!_validator.ValidatePageRange(pageIndex, pageSize))
            {
                throw new ArgumentException($"{nameof(pageIndex)} & {nameof(pageSize)}");
            }

            MembershipUserCollection users = await _sqlStore.GetAllUsers(pageIndex, pageSize).ConfigureAwait(false);

            return users;
        }

        /// <inheritdoc/>
        public async Task<int> GetNumberOfUsersOnline(int timeWindowMinutes)
        {
            int numberOfUsersOnline = await _sqlStore.GetNumberOfUsersOnline(timeWindowMinutes).ConfigureAwait(false);

            return numberOfUsersOnline;
        }

        /// <inheritdoc/>
        public async Task<MembershipUserCollection> FindUsersByName(FindUsersRequest request)
        {
            if (!_validator.ValidateUsername(request.Criteria))
            {
                throw new ArgumentException(nameof(request.Criteria));
            }

            if (!_validator.ValidatePageRange(request.PageIndex, request.PageSize))
            {
                throw new ArgumentException($"{nameof(request.PageIndex)} & {nameof(request.PageSize)}");
            }

            MembershipUserCollection users = await _sqlStore.FindUsersByName(request.Criteria, request.PageIndex, request.PageSize).ConfigureAwait(false);

            return users;
        }

        /// <inheritdoc/>
        public async Task<MembershipUserCollection> FindUsersByEmail(FindUsersRequest request)
        {
            if (!_validator.ValidateEmail(request.Criteria))
            {
                throw new ArgumentException(nameof(request.Criteria));
            }

            if (!_validator.ValidatePageRange(request.PageIndex, request.PageSize))
            {
                throw new ArgumentException($"{nameof(request.PageIndex)} & {nameof(request.PageSize)}");
            }

            MembershipUserCollection users = await _sqlStore.FindUsersByEmail(request.Criteria, request.PageIndex, request.PageSize).ConfigureAwait(false);

            return users;
        }

        /// <inheritdoc/>
        public Task<string> GeneratePassword()
        {
            string password = _encryption.GeneratePassword();
            return Task.FromResult(password);
        }

        private async Task<CheckPasswordResult> CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved)
        {
            GetPasswordWithFormatResult passwordWithFormat = await _sqlStore.GetPasswordWithFormat(username, updateLastLoginActivityDate);

            if (passwordWithFormat.StatusCode != 0 || passwordWithFormat.Password == null || (!passwordWithFormat.IsApproved && failIfNotApproved))
            {
                return new CheckPasswordResult()
                {
                    IsValid = false,
                    PasswordFormat = passwordWithFormat.PasswordFormat,
                    PasswordSalt = passwordWithFormat.PasswordSalt
                };
            }

            string? encodedPassword = _encryption.Encode(password, passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt);

            bool isPasswordCorrect = passwordWithFormat.Password.Equals(encodedPassword);

            if (!isPasswordCorrect || passwordWithFormat.FailedPasswordAttemptCount != 0 || passwordWithFormat.FailedPasswordAnswerAttemptCount != 0)
            {
                await _sqlStore.CheckPassword(username, isPasswordCorrect, updateLastLoginActivityDate, passwordWithFormat.LastLoginDate, passwordWithFormat.LastActivityDate).ConfigureAwait(false);
            }

            return new CheckPasswordResult()
            {
                IsValid = isPasswordCorrect,
                PasswordFormat = passwordWithFormat.PasswordFormat,
                PasswordSalt = passwordWithFormat.PasswordSalt
            };
        }
    }
}