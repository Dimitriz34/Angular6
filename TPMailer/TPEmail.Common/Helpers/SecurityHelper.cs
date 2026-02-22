using TPEmail.BusinessModels.Constants;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using TPEmail.BusinessModels.RequestModels;

namespace TPEmail.Common.Helpers
{
    public static class SecurityHelper
    {
        private static int _saltSize => int.TryParse(Environment.GetEnvironmentVariable("tphashsaltsize"), out var v) ? v : 32;
        private static int _keySize => int.TryParse(Environment.GetEnvironmentVariable("tphashoutputsize"), out var v) ? v : 64;
        private static int _iterations => int.TryParse(Environment.GetEnvironmentVariable("tppbkdf2iterations"), out var v) ? v : 100000;
        private static int _argonDop => int.TryParse(Environment.GetEnvironmentVariable("tpargonparallelism"), out var v) ? v : 8;
        private static int _argonIterations => int.TryParse(Environment.GetEnvironmentVariable("tpargontimecost"), out var v) ? v : 4;
        private static int _argonMemory => int.TryParse(Environment.GetEnvironmentVariable("tpargonmemorysize"), out var v) ? v : 2048;
        private const char SegmentDelimiter = '$';
        private const int DefaultRandomLength = 36;
        private static string _alphanumericCharset => Environment.GetEnvironmentVariable("tprandomcharset") ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly HashAlgorithmName _pdkdf2Algorithm = HashAlgorithmName.SHA512;

        public static string GenerateRandomString(int length = DefaultRandomLength)
        {
            byte[] data = new byte[length];
            RandomNumberGenerator.Fill(data);
            return new string(data.Select(x => _alphanumericCharset[x % _alphanumericCharset.Length]).ToArray());
        }

        public static AppUserCredentials Pbkdf2Hash(string secret)
        {
            var salt = RandomNumberGenerator.GetBytes(_saltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(
                secret,
                salt,
                _iterations,
                _pdkdf2Algorithm,
                _keySize
            );

            string hash = string.Join(
                SegmentDelimiter,
                Convert.ToHexString(key),
                Convert.ToHexString(salt),
                _iterations,
                _pdkdf2Algorithm
            );

            return new AppUserCredentials()
            {
                Hash = hash,
                Salt = Convert.ToBase64String(salt),
                EncryptionKey = Convert.ToBase64String(HashPassword(secret, salt))
            };
        }

        public static bool Pbkdf2Verify(string secret, string hash)
        {
            var segments = hash.Split(SegmentDelimiter);
            var key = Convert.FromHexString(segments[0]);
            var salt = Convert.FromHexString(segments[1]);
            var iterations = int.Parse(segments[2]);
            var algorithm = new HashAlgorithmName(segments[3]);
            var inputSecretKey = Rfc2898DeriveBytes.Pbkdf2(
                secret,
                salt,
                iterations,
                algorithm,
                key.Length
            );

            return key.SequenceEqual(inputSecretKey);
        }

        public static AppUserCredentials GenerateArgonHash(string appSecret)
        {
            if (string.IsNullOrEmpty(appSecret))
            {
                throw new ArgumentNullException(nameof(appSecret), MessageConstants.PasswordCannotBeNullOrEmpty);
            }

            var salt = CreateSalt();
            var hash = HashPassword(appSecret, salt);

            return new AppUserCredentials()
            {
                Hash = Convert.ToBase64String(hash),
                Salt = Convert.ToBase64String(salt),
                EncryptionKey = Convert.ToBase64String(hash)
            };
        }

        public static bool VerifyArgonHash(string password, byte[] salt, byte[] hash)
        {
            var newHash = HashPassword(password, salt);
            return hash.SequenceEqual(newHash);
        }

        public static bool VerifyPbkdf2Raw(string password, byte[] salt, byte[] hash)
        {
            var inputSecretKey = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                _iterations,
                _pdkdf2Algorithm,
                hash.Length
            );

            return hash.SequenceEqual(inputSecretKey);
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), MessageConstants.PasswordCannotBeNullOrEmpty);
            }

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = _argonDop,
                Iterations = _argonIterations,
                MemorySize = _argonMemory
            };

            return argon2.GetBytes(_keySize);
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;

            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hashBytes = Convert.FromBase64String(hash);

            if (VerifyArgonHash(password, saltBytes, hashBytes))
                return true;

            if (VerifyPbkdf2Raw(password, saltBytes, hashBytes))
                return true;

            return false;
        }

        private static byte[] CreateSalt()
        {
            return RandomNumberGenerator.GetBytes(_saltSize);
        }
    }
}
