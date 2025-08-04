

namespace VoucherGrpc.Extensions
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(VoucherDbContext context)
        {
            if (!context.VoucherTemplates.Any())
            {
                var templates = new List<VoucherTemplate>
                {
                    new VoucherTemplate
                    {
                        Id = Guid.NewGuid(),
                        CodePrefix = "WELCOME10",
                        DisplayName = "Welcome Discount",
                        Description = "10% off for new users",
                        DiscountAmount = 10,
                        DiscountType = DiscountType.Percent,
                        ValidDays = 7,
                        AutoIssue = true,
                        RuleJson = "{ \"user.isNew\": true }", 
                        CreatedAt = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddDays(7)
                    },
                    new VoucherTemplate
                    {
                        Id = Guid.NewGuid(),
                        CodePrefix = "BIRTHDAY50",
                        DisplayName = "Birthday Bonus",
                        Description = "50,000 VND off on your birthday",
                        DiscountAmount = 50000,
                        DiscountType = DiscountType.Fixed,
                        ValidDays = 3,
                        AutoIssue = true,
                        RuleJson = "{ \"==\": [ { \"var\": \"user.birthMonth\" }, { \"var\": \"currentMonth\" } ] }",
                        CreatedAt = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddDays(3)
                    }
                };

                await context.VoucherTemplates.AddRangeAsync(templates);
                await context.SaveChangesAsync();

                var template = templates[0];
                await context.SaveChangesAsync();
            }
        }
    }
}
