using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationAPI.Extensions
{
    public static class DatabaseMigrationExtensions
    {
        public static async Task<IHost> EnsureDatabaseMigratedAndSeeded(this IHost app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed users if none exist
            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    new User
                    {
                        Username = "yummyadmin",
                        PasswordHash = PasswordHasher.HashPassword("admin123"),
                        Role = UserRole.Admin,
                        Email = "yummyadmin@test.example"
                    },
                    new User
                    {
                        Username = "yummyuser",
                        PasswordHash = PasswordHasher.HashPassword("user123"),
                        Role = UserRole.User,
                        Email = "yummyuser@test.example"
                    }
                };

                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }

            return app;
        }

        public static async Task<IHost> ClearSeedData(this IHost app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var yummyUsers = await context.Users
                .Where(x => x.Username.Contains("yummy"))
                .ToListAsync();

            if (yummyUsers.Any())
            {
                context.Users.RemoveRange(yummyUsers);
                await context.SaveChangesAsync();
            }

            return app;
        }
    }
}
