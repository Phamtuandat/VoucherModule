namespace AuthenticationAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(string userId, string userName, UserRole role, DateTime expirationDate);
        bool ValidateToken(string token, out string userId, out string userName, out string role);
        void InvalidateToken(string token);
        DateTime GetTokenExpirationDate(string token);
        bool IsTokenValid(string token);
    }
}
