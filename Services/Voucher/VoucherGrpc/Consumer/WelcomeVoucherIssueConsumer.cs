
using Contract.VoucherEvents;

namespace VoucherGrpc.Consumer
{
    public class WelcomeVoucherIssueConsumer(VoucherDbContext context, IPublishEndpoint publish, ILogger<WelcomeVoucherIssueConsumer> logger) : IConsumer<WelcomeVoucherIssue>
    {
        private readonly ILogger<WelcomeVoucherIssueConsumer> _logger = logger;
        private readonly VoucherDbContext _context = context;
        private readonly IPublishEndpoint _publish = publish;
        public async Task  Consume(ConsumeContext<WelcomeVoucherIssue> context)
        {
            var message = context.Message;
            _logger.LogInformation("Received VoucherIssue message for UserId: {UserId}, TemlateCode: {TemplateCode}", message.UserId, message.TemplateCode);
            var template = await _context.VoucherTemplates.FirstOrDefaultAsync(x => x.CodePrefix == message.TemplateCode, context.CancellationToken);
            if(template == null)
            {
                _logger.LogWarning("Voucher template with code {TemplateCode} not found for UserId: {UserId}", message.TemplateCode, message.UserId);
                return;
            }
            var voucher = new VoucherEntity
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Code = $"{template.CodePrefix}-{Guid.NewGuid().ToString()[..8]}",
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(template.ValidDays),
                IsPublic = template.AutoIssue,
                Amount = template.DiscountType == DiscountType.Percent ? template.DiscountAmount : 0,
                IsRedeemed = false,
                RedeemedAt = null,
                RedeemedByUserId = null,
                UserId = message.UserId
            };
            _logger.LogInformation("Creating voucher {VoucherCode} for UserId: {UserId}", voucher.Code, message.UserId);
            _context.Vouchers.Add(voucher);
            try
            {
                await _context.SaveChangesAsync(context.CancellationToken);
                await _publish.Publish(new VoucherIssuedEvent(message.UserId, voucher.Code, voucher.CreatedAt), context.CancellationToken);
                _logger.LogInformation("Voucher {VoucherCode} created successfully for UserId: {UserId}", voucher.Code, message.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher {VoucherCode} for UserId: {UserId}", voucher.Code, message.UserId);
            }
        }
    }
}
