using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter
{
    public class SqlMembershipService : ISqlMembershipService
    {
        private const int SALT_SIZE = 16;
        private const int PASSWORD_SIZE = 14;

        private static readonly char[] startingChars = ['<', '&'];
        private static readonly char[] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        private readonly ISqlMembershipSettings _settings;
        private readonly ISqlMembershipValidator _validator;
        private readonly ISqlMembershipStore _sqlStore;

        public SqlMembershipService(
            string sqlConnectionString)
        {
            _settings = new SqlMembershipSettings(sqlConnectionString);
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipStore(_settings);
        }

        public SqlMembershipService(
            SqlMembershipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _validator = new SqlMembershipValidator(_settings);
            _sqlStore = new SqlMembershipStore(_settings);
        }

        internal SqlMembershipService(
            ISqlMembershipStore sqlStore,
            ISqlMembershipValidator validator,
            ISqlMembershipSettings settings)
        {
            _settings = settings;
            _sqlStore = sqlStore;
            _validator = validator;
        }

        public async Task<CreateUserResult> CreateUser(CreateUserRequest request)
        {
            if (!_validator.ValidatePassword(request.Password))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidPassword);
            }

            string passwordSalt = GenerateSalt();
            string password = EncodePassword(request.Password, (int)_settings.PasswordFormat, passwordSalt);

            if (!_validator.ValidatePassword(password))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidPassword);
            }

            if (!_validator.ValidatePasswordAnswer(request.PasswordAnswer))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidAnswer);
            }

            string? encodedPasswordAnswer = request.PasswordAnswer != null ?
                EncodePassword(request.PasswordAnswer.ToLower(CultureInfo.InvariantCulture), (int)_settings.PasswordFormat, passwordSalt) : null;

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidAnswer);
            }

            if (!_validator.ValidateUsername(request.Username))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidUserName);
            }

            if (!_validator.ValidateEmail(request.Email))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidEmail);
            }

            if (!_validator.ValidatePasswordQuestion(request.PasswordQuestion))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidQuestion);
            }

            if (!_validator.ValidatePasswordComplexity(request.Password))
            {
                return new CreateUserResult(MembershipCreateStatus.InvalidPassword);
            }

            CreateUserResult createResult = await _sqlStore.CreateUser(
                request.ProviderUserKey,
                request.Username,
                request.Password,
                passwordSalt,
                request.Email,
                request.PasswordQuestion,
                request.PasswordAnswer,
                request.IsApproved);

            return createResult;
        }

        public async Task<bool> ChangePasswordQuestionAndAnswer(ChangePasswordQuestionAndAnswerRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException("Invalid username", nameof(request.Username));
            }

            if (!_validator.ValidatePassword(request.Password))
            {
                throw new ArgumentException("Invalid password", nameof(request.Password));
            }

            var checkPassword = await CheckPassword(request.Username, request.Password, false, false);

            if (!checkPassword.IsValid)
            {
                return false;
            }

            string? encodedPasswordAnswer = request.NewPasswordAnswer != null ?
                EncodePassword(request.NewPasswordAnswer.ToLower(CultureInfo.InvariantCulture), checkPassword.PasswordFormat, checkPassword.PasswordSalt) : null;

            if (!_validator.ValidatePasswordQuestion(request.NewPasswordQuestion))
            {
                throw new ArgumentException("Invalid new password question.", nameof(request.NewPasswordQuestion));
            }

            if (!_validator.ValidatePasswordAnswer(request.NewPasswordQuestion))
            {
                throw new ArgumentException("Invalid new password answer.", nameof(request.NewPasswordQuestion));
            }

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                throw new ArgumentException("Invalid new password answer.", nameof(request.NewPasswordQuestion));
            }

            bool isUpdated = await _sqlStore.ChangePasswordQuestionAndAnswer(request.Username, request.Password, request.NewPasswordQuestion, encodedPasswordAnswer);

            return isUpdated;
        }

        public async Task<bool> ChangePassword(ChangePasswordRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException("Invalid username", nameof(request.Username));
            }

            if (!_validator.ValidatePassword(request.NewPassword))
            {
                throw new ArgumentException("Invalid new password", nameof(request.NewPassword));
            }

            if (!_validator.ValidatePassword(request.OldPassword))
            {
                throw new ArgumentException("Invalid old password", nameof(request.OldPassword));
            }

            var checkPassword = await CheckPassword(request.Username, request.OldPassword, false, false);

            if (!checkPassword.IsValid)
            {
                return false;
            }

            if (!_validator.ValidatePasswordComplexity(request.NewPassword))
            {
                throw new ArgumentException("Invalid new password", nameof(request.NewPassword));
            }

            string encodedPassword = EncodePassword(request.NewPassword, checkPassword.PasswordFormat, checkPassword.PasswordSalt);

            if (!_validator.ValidatePassword(encodedPassword))
            {
                throw new ArgumentException("Invalid new password", nameof(request.NewPassword));
            }

            bool isPasswordChanged = await _sqlStore.ChangePassword(request.Username, encodedPassword, checkPassword.PasswordSalt, checkPassword.PasswordFormat);

            return isPasswordChanged;
        }

        public async Task<string> ResetPassword(ResetPasswordRequest request)
        {
            if (!_settings.EnablePasswordReset)
            {
                throw new NotSupportedException("Password resets not configured: set EnablePasswordReset to \"true\".");
            }

            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException("Invalid username", nameof(request.Username));
            }

            var passwordWithFormat = await _sqlStore.GetPasswordWithFormat(request.Username, false);

            if (passwordWithFormat.Status != 0)
            {
                var exceptionText = GetExceptionText(passwordWithFormat.Status);

                throw (passwordWithFormat.Status is (>= 2 and <= 6) or 99)
                    ? new MembershipPasswordException(exceptionText) : new ProviderException(exceptionText);
            }

            string? encodedPasswordAnswer = request.PasswordAnswer;

            if (!string.IsNullOrEmpty(request.PasswordAnswer))
            {
                encodedPasswordAnswer = EncodePassword(request.PasswordAnswer.ToLower(CultureInfo.InvariantCulture), passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt);
            }

            if (!_validator.ValidatePasswordAnswer(encodedPasswordAnswer))
            {
                throw new ArgumentException("Invalid password answer", nameof(request.PasswordAnswer));
            }

            string newPassword = await GeneratePassword();
            string newPasswordEncoded = EncodePassword(newPassword, passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt);

            await _sqlStore.ResetPassword(request.Username, newPasswordEncoded, passwordWithFormat.PasswordSalt, encodedPasswordAnswer, passwordWithFormat.PasswordFormat);

            return newPassword;
        }

        public async Task UpdateUser(MembershipUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!_validator.ValidateUsername(user.UserName))
            {
                throw new ArgumentException(nameof(user.UserName));
            }

            if (!_validator.ValidateEmail(user.Email))
            {
                throw new ArgumentException(nameof(user.Email));
            }

            await _sqlStore.UpdateUser(user);
        }

        public async Task<bool> ValidateUser(ValidateUserRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException(nameof(request.Username));
            }

            if (!_validator.ValidatePassword(request.Password))
            {
                throw new ArgumentException(nameof(request.Password));
            }

            var isValidPassword = await CheckPassword(request.Username, request.Password, true, true);

            return isValidPassword.IsValid;
        }

        public async Task<bool> UnlockUser(string username)
        {
            username = username?.Trim() ?? string.Empty;

            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            bool isUnlocked = await _sqlStore.UnlockUser(username);

            return isUnlocked;
        }

        public async Task<MembershipUser?> GetUser(string username, bool updateLastActivity)
        {
            username = username?.Trim() ?? string.Empty;

            if (!_validator.ValidateUsername(username))
            {
                throw new ArgumentException(nameof(username));
            }

            MembershipUser? user = await _sqlStore.GetUser(username, updateLastActivity);

            return user;
        }

        public async Task<string?> GetUserNameByEmail(string? email)
        {
            if (!_validator.ValidateEmail(email))
            {
                throw new ArgumentException(nameof(email));
            }

            string? userName = await _sqlStore.GetUsernameByEmail(email);

            return userName;
        }

        public async Task<bool> DeleteUser(DeleteUserRequest request)
        {
            if (!_validator.ValidateUsername(request.Username))
            {
                throw new ArgumentException(nameof(request.Username));
            }

            bool isDeleted = await _sqlStore.DeleteUser(request.Username, request.DeleteAllRelatedData);

            return isDeleted;
        }

        public async Task<MembershipUserCollection> GetAllUsers(int pageIndex, int pageSize)
        {
            if (!_validator.ValidatePageRange(pageIndex, pageSize))
            {
                throw new ArgumentOutOfRangeException("PageIndex and PageSize");
            }

            var users = await _sqlStore.GetAllUsers(pageIndex, pageSize);

            return users;
        }

        public async Task<int> GetNumberOfUsersOnline(int timeWindowMinutes)
        {
            int numberOfUsersOnline = await _sqlStore.GetNumberOfUsersOnline(timeWindowMinutes);

            return numberOfUsersOnline;
        }

        public async Task<MembershipUserCollection> FindUsersByName(FindUsersRequest request)
        {
            if (!_validator.ValidateUsername(request.Criteria))
            {
                throw new ArgumentException(nameof(request.Criteria));
            }

            if (!_validator.ValidatePageRange(request.PageIndex, request.PageSize))
            {
                throw new ArgumentOutOfRangeException("PageIndex and PageSize");
            }

            var users = await _sqlStore.FindUsersByName(request.Criteria, request.PageIndex, request.PageSize);

            return users;
        }

        public async Task<MembershipUserCollection> FindUsersByEmail(FindUsersRequest request)
        {
            if (!_validator.ValidateEmail(request.Criteria))
            {
                throw new ArgumentException(nameof(request.Criteria));
            }

            if (!_validator.ValidatePageRange(request.PageIndex, request.PageSize))
            {
                throw new ArgumentOutOfRangeException("PageIndex and PageSize");
            }

            var users = await _sqlStore.FindUsersByEmail(request.Criteria, request.PageIndex, request.PageSize);

            return users;
        }

        public Task<string> GeneratePassword()
        {
            int length = Math.Max(_settings.MinRequiredPasswordLength, PASSWORD_SIZE);

            StringBuilder passwordBuilder = new(length);
            Random rand = new();

            string password;
            bool isDangerous;

            do
            {
                for (int i = 0; i < length; i++)
                {
                    int randType = rand.Next(0, 4);

                    var c = randType switch
                    {
                        0 => (char)rand.Next('0', '9' + 1),
                        1 => (char)rand.Next('A', 'Z' + 1),
                        2 => (char)rand.Next('a', 'z' + 1),
                        _ => punctuations[rand.Next(0, punctuations.Length)],
                    };

                    passwordBuilder.Append(c);
                }

                password = passwordBuilder.ToString();
                isDangerous = IsDangerousString(password);

                if (isDangerous)
                {
                    passwordBuilder.Clear();
                }
            }
            while (isDangerous);

            return Task.FromResult(password);
        }

        private async Task<CheckPasswordResult> CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved)
        {
            GetPasswordWithFormatResponse passwordWithFormat = await _sqlStore.GetPasswordWithFormat(username, updateLastLoginActivityDate);

            if (passwordWithFormat.Status != 0 || passwordWithFormat.Password == null || (!passwordWithFormat.IsApproved && failIfNotApproved))
            {
                return new CheckPasswordResult()
                {
                    IsValid = false,
                    PasswordFormat = passwordWithFormat.PasswordFormat,
                    PasswordSalt = passwordWithFormat.PasswordSalt
                };
            }

            string encodedPassword = EncodePassword(password, passwordWithFormat.PasswordFormat, passwordWithFormat.PasswordSalt);

            bool isPasswordCorrect = passwordWithFormat.Password.Equals(encodedPassword);

            if (!isPasswordCorrect || passwordWithFormat.FailedPasswordAttemptCount != 0 || passwordWithFormat.FailedPasswordAnswerAttemptCount != 0)
            {
                await _sqlStore.CheckPassword(username, isPasswordCorrect, updateLastLoginActivityDate, passwordWithFormat.LastLoginDate, passwordWithFormat.LastActivityDate);
            }

            return new CheckPasswordResult()
            {
                IsValid = isPasswordCorrect,
                PasswordFormat = passwordWithFormat.PasswordFormat,
                PasswordSalt = passwordWithFormat.PasswordSalt
            };
        }

        private string EncodePassword(string? password, int passwordFormat, string? salt)
        {
            if (passwordFormat != 1 || string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(password))
            {
                return password ?? string.Empty;
            }

            byte[] bIn = Encoding.Unicode.GetBytes(password);
            byte[] bSalt = Convert.FromBase64String(salt);

            HashAlgorithm hm = _settings.HashAlgorithm switch
            {
                HashAlgorithmType.SHA1 => SHA1.Create(),
                HashAlgorithmType.SHA512 => SHA512.Create(),
                HashAlgorithmType.SHA384 => SHA384.Create(),
                HashAlgorithmType.SHA256 => SHA256.Create(),
                HashAlgorithmType.MD5 => MD5.Create(),
                _ => SHA1.Create(),
            };

            byte[] bAll = new byte[bSalt.Length + bIn.Length];

            Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
            Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);

            byte[] bRet = hm.ComputeHash(bAll);

            return Convert.ToBase64String(bRet);
        }

        private static string GenerateSalt()
        {
            byte[] buf = new byte[SALT_SIZE];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buf);
            }

            return Convert.ToBase64String(buf);
        }

        private static bool IsDangerousString(string s)
        {
            foreach (char c in startingChars)
            {
                int index = s.IndexOf(c);
                if (index >= 0 && index < s.Length - 1)
                {
                    char nextChar = s[index + 1];

                    if ((c == '<' && (char.IsLetter(nextChar) || nextChar == '!' || nextChar == '/' || nextChar == '?')) || (c == '&' && nextChar == '#'))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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