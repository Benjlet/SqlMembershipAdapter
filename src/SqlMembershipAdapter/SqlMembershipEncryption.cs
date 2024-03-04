using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SqlMembershipAdapter.Abstractions;
using SqlMembershipAdapter.Models;

namespace SqlMembershipAdapter
{
    internal class SqlMembershipEncryption : ISqlMembershipEncryption
    {
        private const int SALT_SIZE = 16;
        private const int PASSWORD_SIZE = 14;

        private static readonly char[] startingChars = ['<', '&'];
        private static readonly char[] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        private readonly ISqlMembershipSettings _settings;

        public SqlMembershipEncryption(
            ISqlMembershipSettings settings)
        {
            _settings = settings;
        }

        public string GeneratePassword()
        {
            int length = Math.Max(_settings.MinRequiredPasswordLength, PASSWORD_SIZE);

            string password;
            byte[] buf;
            char[] cBuf;
            int count;

            do
            {
                buf = new byte[length];
                cBuf = new char[length];
                count = 0;

                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                rng.GetBytes(buf);

                for (int iter = 0; iter < length; iter++)
                {
                    int i = buf[iter] % 87;
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

                if (count < _settings.MinRequiredNonAlphanumericCharacters)
                {
                    int j, k;
                    Random rand = new();

                    for (j = 0; j < _settings.MinRequiredNonAlphanumericCharacters - count; j++)
                    {
                        do
                        {
                            k = rand.Next(0, length);
                        }
                        while (!char.IsLetterOrDigit(cBuf[k]));

                        cBuf[k] = punctuations[rand.Next(0, punctuations.Length)];
                    }
                }

                password = new string(cBuf);
            }
            while (IsDangerousString(password));

            return password;
        }

        public string? Encode(string? text, int format, string? salt)
        {
            if (format != 1 || string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            text = text.ToLower(CultureInfo.InvariantCulture);

            byte[] bIn = Encoding.Unicode.GetBytes(text);
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

        public string GenerateSalt()
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
            for (int i = 0; i < startingChars.Length; i++)
            {
                char c = startingChars[i];
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
    }
}