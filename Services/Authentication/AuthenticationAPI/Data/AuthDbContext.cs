using BuildingBlocks.Contract.UserEvents;
using MassTransit;

namespace AuthenticationAPI.Data
{
    public class AuthDbContext : DbContext
    {
        private readonly IPublishEndpoint _publish;
        public AuthDbContext(DbContextOptions<AuthDbContext> options, IPublishEndpoint publish) : base(options)
        {
            _publish = publish;
        }
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    }
}
