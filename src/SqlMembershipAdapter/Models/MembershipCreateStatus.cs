namespace SqlMembershipAdapter.Models
{
    /// <summary>
    /// The status of the user creation request.
    /// </summary>
    public enum MembershipCreateStatus
    {
        /// <summary>
        /// Success.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Invalid user name.
        /// </summary>
        InvalidUserName = 1,

        /// <summary>
        /// New password was not accepted (invalid format).
        /// </summary>
        InvalidPassword = 2,

        /// <summary>
        /// New question was not accepted (invalid format).
        /// </summary>
        InvalidQuestion = 3,

        /// <summary>
        /// New password answer was not acceppted (invalid format).
        /// </summary>
        InvalidAnswer = 4,

        /// <summary>
        /// New email was not accepted (invalid format).
        /// </summary>
        InvalidEmail = 5,

        /// <summary>
        /// Username already exists.
        /// </summary>
        DuplicateUserName = 6,

        /// <summary>
        /// Email already exists.
        /// </summary>
        DuplicateEmail = 7,

        /// <summary>
        /// Provider rejected user (for some user-specific reason).
        /// </summary>
        UserRejected = 8,

        /// <summary>
        /// New provider user key was not accepted (invalid format).
        /// </summary>
        InvalidProviderUserKey = 9,

        /// <summary>
        /// Provider user key already exists.
        /// </summary>
        DuplicateProviderUserKey = 10,

        /// <summary>
        /// Provider-specific error (couldn't map onto this enumeration).
        /// </summary>
        ProviderError = 11
    }
}