using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuthenticationAPI.Data.DatabaseExtensions
{
    public class UserCreatedInterceptor(IPublishEndpoint publish, ILogger<UserCreatedInterceptor> logger) : SaveChangesInterceptor
    {
        private readonly IPublishEndpoint _publish = publish;
        private readonly ILogger<UserCreatedInterceptor> _logger = logger;

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("✅ UserCreatedInterceptor invoked");

            var context = eventData.Context;
            if (context is AuthDbContext authDbContext)
            {
                var users = authDbContext.ChangeTracker.Entries<User>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity);
                foreach (var user in users)
                {
                    _logger.LogInformation("Publishing UserRegistered event for user: {UserId}", user.Id);
                    await _publish.Publish(new UserRegistered(user.Id, user.Email, user.FirstName + user.LastName), cancellationToken);
                }
            }
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
