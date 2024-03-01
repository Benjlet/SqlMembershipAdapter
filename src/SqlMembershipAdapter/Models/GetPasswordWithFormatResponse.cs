﻿namespace SqlMembershipAdapter.Models
{
    public class GetPasswordWithFormatResponse
    {
        public int Status { get; set; }
        public string? Password { get; set; }
        public int PasswordFormat { get; set; }
        public string? PasswordSalt { get; set; }
        public int FailedPasswordAttemptCount { get; set; }
        public int FailedPasswordAnswerAttemptCount { get; set; }
        public bool IsApproved { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastActivityDate { get; set; }
    }
}