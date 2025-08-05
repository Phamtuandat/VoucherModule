
using Contract.VoucherEvents;

namespace VoucherGrpc.Consumer
{
    public class WelcomeVoucherIssueConsumer(IVoucherService voucherService, IVoucherTemplateService tempService, IPublishEndpoint publish, ILogger<WelcomeVoucherIssueConsumer> logger) : IConsumer<WelcomeVoucherIssue>
    {
        private readonly ILogger<WelcomeVoucherIssueConsumer> _logger = logger;
        private readonly IVoucherTemplateService _tempService = tempService;
        private readonly IVoucherService _voucherService  = voucherService;
        private readonly IPublishEndpoint _publish = publish;
        public async Task Consume(ConsumeContext<WelcomeVoucherIssue> context)
        {
            var message = context.Message;
            if (message == null || message.UserId == Guid.Empty || string.IsNullOrEmpty(message.TemplateCode))
            {
                _logger.LogWarning("Received invalid WelcomeVoucherIssue message: {Message}", message);
                return;
            }
            _logger.LogInformation("Received VoucherIssue message for UserId: {UserId}, TemlateCode: {TemplateCode}", message.UserId, message.TemplateCode);
            var template = await _tempService.GetTemplateByPrefixAsync(message.TemplateCode, context.CancellationToken);
            if (template == null)
            {
                _logger.LogWarning("Voucher template with code {TemplateCode} not found for UserId: {UserId}", message.TemplateCode, message.UserId);
                return;
            }
            var isHasVoucher = await _voucherService.HasVoucher(message.UserId.ToString(), template.Id);
            if (isHasVoucher)
            {
                _logger.LogInformation("User {UserId} already has a voucher for template {TemplateCode}", message.UserId, message.TemplateCode);
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
            var ruleContext = new
            {
                user = new
                {
                    isNew = true
                }
            };
            try
            {
                var newVoucher = await _voucherService.IssueVoucherAsync(message.UserId.ToString(), template, ruleContext);
                if (newVoucher != null)
                {
                    await _publish.Publish(new VoucherIssuedEvent(message.UserId, voucher.Code, voucher.CreatedAt), context.CancellationToken);
                    _logger.LogInformation("Voucher {VoucherCode} created successfully for UserId: {UserId}", voucher.Code, message.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher {VoucherCode} for UserId: {UserId}", voucher.Code, message.UserId);
            }
        }
    }
}
