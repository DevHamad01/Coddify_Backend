using System.Security.Cryptography;
using System.Text;

namespace CommonOperations.Security
{
    /// <summary>
    /// Salted PBKDF2-SHA256 password hashes stored as "pbkdf2$iterations$salt$hash" (Base64).
    /// Hashes made by the first auth version (unsalted SHA-256) still verify and are flagged for re-hashing.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2";
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <param name="needsRehash">True when the stored hash uses the old format and should be replaced.</param>
        public static bool Verify(string password, string stored, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            var parts = stored.Split('$');
            if (parts.Length == 4 && parts[0] == Prefix && int.TryParse(parts[1], out var iterations))
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                needsRehash = iterations < Iterations;
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            // Legacy: SHA-256 of the Base64 of the password, hex encoded.
            var legacy = LegacyHash(password);
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(stored)))
            {
                needsRehash = true;
                return true;
            }
            return false;
        }

        private static string LegacyHash(string password)
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(base64));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
