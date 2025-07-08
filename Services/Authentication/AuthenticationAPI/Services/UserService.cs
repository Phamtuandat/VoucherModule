
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;
namespace AuthenticationAPI.Services;

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
        user.PasswordHash = HashPassword(password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return HashPassword(password) == passwordHash;
    }

    public string HashPassword(string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes("static-salt-123"); // nên dùng salt ngẫu nhiên và lưu riêng
        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password, salt,
            KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        return hashed;
    }
}
