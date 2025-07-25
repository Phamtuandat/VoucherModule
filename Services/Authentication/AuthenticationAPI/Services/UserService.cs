using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using AuthenticationAPI.Models;
using AuthenticationAPI.Data;

namespace AuthenticationAPI.Services
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _db;

        public UserService(AuthDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> CreateAsync(User user, string password)
        {
            if (await _db.Users.AnyAsync(u => u.Username == user.Username))
                throw new InvalidOperationException("Username already exists.");

            user.PasswordHash = PasswordHasher.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return PasswordHasher.VerifyPassword(password, passwordHash);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _db.Users.ToListAsync();
        }
        public async Task<RefreshToken> CreateRefreshTokenAsync(User user)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<RefreshToken?> GetValidRefreshTokenAsync(string token)
        {
            return await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt =>
                    rt.Token == token &&
                    rt.ExpiryDate > DateTime.UtcNow &&
                    !rt.IsRevoked &&
                    !rt.IsUsed);
        }

        public async Task InvalidateRefreshTokenAsync(string token)
        {
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (existing != null)
            {
                existing.IsUsed = true;
                existing.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }

    }
}
