namespace SqlMembershipAdapter.Extensions
{
    internal static class SqlMembershipExtensions
    {
        public static string ToProviderErrorText(this int status) => status switch
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

        public static DateTime RoundToSeconds(this DateTime utcDateTime) =>
            new(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day, utcDateTime.Hour, utcDateTime.Minute, utcDateTime.Second, DateTimeKind.Utc);

        public static bool IsBadPasswordStatus(this int status) =>
            status >= 2 && status <= 6 || status == 99;
    }
}
