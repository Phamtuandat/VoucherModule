

namespace VoucherGrpc.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly VoucherDbContext _context;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(VoucherDbContext context, ILogger<UserRegisteredConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var message = context.Message;

        _logger.LogInformation("Received UserRegistered event for UserId: {UserId}", message.UserId);

        // Load auto-issue templates
        var templates = await _context.VoucherTemplates
            .Where(t => t.AutoIssue)
            .ToListAsync();

        foreach (var template in templates)
        {
            var ruleContext = new
            {
                user = new
                {
                    isNew = true,
                    email = message.Email,
                    fullName = message.FullName
                }
            };

            var isValid = RuleEvaluator.Evaluate(template.RuleJson, ruleContext);

            if (isValid)
            {
                var voucher = new VoucherEntity
                {
                    Code = $"{template.CodePrefix}-{Guid.NewGuid().ToString("N")[..6]}",
                    Amount = template.DiscountAmount,
                    ExpiryDate = DateTime.UtcNow.AddDays(template.ValidDays),
                    TemplateId = template.Id,
                    IsRedeemed = false
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Issued voucher {Code} to user {UserId}", voucher.Code, message.UserId);
                break; // Stop after first matching template
            }
        }
    }
}
