using Newtonsoft.Json;

namespace VoucherGrpc.Services
{
    public class VoucherService(ILogger<VoucherService> logger, VoucherDbContext context) : IVoucherService
    {
        private readonly VoucherDbContext _context = context;
        private readonly ILogger<VoucherService> _logger = logger;



        public async Task<bool> HasVoucher(string userId, Guid templateId)
        {
            return await _context.Vouchers
                .AnyAsync(v => v.TemplateId == templateId && v.RedeemedByUserId == userId);
        }

        public async Task<VoucherEntity?> IssueVoucherAsync(string userId, VoucherTemplate template, object ruleContext)
        {
            if (!RuleEvaluator.Evaluate(template.RuleJson, ruleContext))
            {
                var result = RuleEvaluator.Evaluate(template.RuleJson, ruleContext);
                var isBool = result is bool;
                RuleDebugger.Debug(template.RuleJson, ruleContext);

                _logger.LogWarning(
                    "Rule evaluation failed. Result: {Result}, Type: {Type}, Rule: {Rule}, Context: {Context}",
                    result,
                    result.GetType().Name,
                    template.RuleJson, 
                    JsonConvert.SerializeObject(ruleContext)  
                );
                return null;
            };

            if (await HasVoucher(userId, template.Id))
            {
                _logger.LogWarning("User {UserId} already has a voucher for template {TemplateId}", userId, template.Id);
                return null;
            };

            var voucher = new VoucherEntity
            {
                Id = Guid.NewGuid(),
                Code = GenerateVoucherCode(),
                Amount = template.DiscountAmount,
                TemplateId = template.Id,
                RedeemedByUserId = userId,
                IsRedeemed = false,
                ExpiryDate = template.ExpiryDate
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return voucher;
        }

        public async Task<VoucherEntity?> RedeemVoucherAsync(string code, string userId)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == code && !v.IsRedeemed);

            if (voucher == null || voucher.ExpiryDate < DateTime.UtcNow)
                return null;

            voucher.IsRedeemed = true;
            voucher.RedeemedAt = DateTime.UtcNow;
            voucher.RedeemedByUserId = userId;

            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<IEnumerable<VoucherEntity>> GetVouchersByUser(string userId)
        {
            return await _context.Vouchers
                .Where(v => v.RedeemedByUserId == userId)
                .ToListAsync();
        }

        private string GenerateVoucherCode()
        {
            return $"VC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
