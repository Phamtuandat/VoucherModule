using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace AuthenticationAPI.Extensions
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16); // salt ngẫu nhiên 16 byte

            byte[] hashed = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            // Lưu salt + hash bằng Base64 (salt:hash)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashed)}";
        }

        public static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            byte[] actualHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
    }

}
