

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
                        DiscountAmount = 10,
                        DiscountType = "percent",
                        ValidDays = 7,
                        AutoIssue = true,
                        RuleJson = "{ \"user.isNew\": true }",
                        CreatedAt = DateTime.UtcNow
                    },
                    new VoucherTemplate
                    {
                        Id = Guid.NewGuid(),
                        CodePrefix = "HOLIDAY25",
                        DisplayName = "Holiday Promo",
                        DiscountAmount = 25,
                        DiscountType = "fixed",
                        ValidDays = 5,
                        AutoIssue = true,
                        RuleJson = "{ \"date.isHoliday\": true }",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.VoucherTemplates.AddRangeAsync(templates);
                await context.SaveChangesAsync();

                // Add vouchers for first template
                var template = templates[0]; // WELCOME10
                var vouchers = new List<VoucherEntity>
                {
                    new VoucherEntity
                    {
                        Code = "WELCOME10-USER123",
                        Amount = 10,
                        IsRedeemed = false,
                        ExpiryDate = DateTime.UtcNow.AddDays(template.ValidDays),
                        TemplateId = template.Id
                    },
                    new VoucherEntity
                    {
                        Code = "WELCOME10-USER456",
                        Amount = 10,
                        IsRedeemed = true,
                        ExpiryDate = DateTime.UtcNow.AddDays(template.ValidDays),
                        TemplateId = template.Id
                    }
                };

                await context.Vouchers.AddRangeAsync(vouchers);
                await context.SaveChangesAsync();
            }
        }
    }
}
