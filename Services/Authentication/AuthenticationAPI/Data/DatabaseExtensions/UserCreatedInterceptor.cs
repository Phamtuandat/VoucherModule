using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuthenticationAPI.Data.DatabaseExtensions
{
    public class UserCreatedInterceptor(IPublishEndpoint publish, ILogger<UserCreatedInterceptor> logger) : SaveChangesInterceptor
    {
        private readonly IPublishEndpoint _publish = publish;
        private readonly ILogger<UserCreatedInterceptor> _logger = logger;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
             DbContextEventData eventData,
             InterceptionResult<int> result,
             CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("✅ UserCreatedInterceptor SavingChangesAsync invoked");

            if (eventData.Context is AuthDbContext authDbContext)
            {
                var users = authDbContext.ChangeTracker.Entries<User>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity)
                    .ToList();

                foreach (var user in users)
                {
                    var message = new UserRegistered(user.Id, user.Email, user.FirstName + user.LastName);
                    await _publish.Publish(message, cancellationToken);
                    _logger.LogInformation("✅ UserRegistered event queued for user: {CorrelationId}", message.CorrelationId);
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
