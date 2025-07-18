
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
        user.PasswordHash = PasswordHasher.HashPassword(password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return PasswordHasher.HashPassword(password) == passwordHash;
    }

   
}
