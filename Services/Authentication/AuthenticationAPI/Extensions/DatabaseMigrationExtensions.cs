namespace AuthenticationAPI.Extensions
{
    public static class DatabaseMigrationExtensions
    {
        public static IHost EnsureDatabaseMigratedAndSeeded(this IHost app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            context.Database.Migrate();

            // 🍩 Seed yummy data
            if (context.Users.Any(x => x.Username.Contains("yummy")) || !context.Users.Any())
            {
                
                List<User> yummyUsers = context.Users.Where(x => x.Username.Contains("yummy"))
                    .ToList();
                context.Users.RemoveRange(yummyUsers);

                context.Users.AddRange(
                    new User
                    {
                        Username = "yummyadmin",
                        PasswordHash = PasswordHasher.HashPassword("admin123"),
                        Role = UserRole.Admin,
                        Email = "yummyadmin@test.example",
                        
                        
                    },
                    new User
                    {
                        Username = "yummyuser",
                        PasswordHash = PasswordHasher.HashPassword("user123"),
                        Role = UserRole.User,
                        Email = "yummyuser@test.example"
                    }
                );
                context.SaveChanges();
            }

            return app;
        }
    }
}
