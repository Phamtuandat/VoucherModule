
namespace AuthenticationAPI.Extensions
{
    public static class UserMapper
    {
        public static User ToEntity(UserRegisterRequest request)
        {
            return new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                Email = request.Email,
                BirthDate = request.BirthDate,
            };
        }
    }
}
