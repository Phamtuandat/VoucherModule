namespace AuthenticationAPI.Services
{
    public interface IUserService
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(User user, string password);
        bool VerifyPassword(string password, string passwordHash);
        string HashPassword(string password);
    }
}
