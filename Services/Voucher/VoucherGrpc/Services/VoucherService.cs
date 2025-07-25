namespace VoucherGrpc.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly VoucherDbContext _context;

        public VoucherService(VoucherDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasVoucher(string userId, Guid templateId)
        {
            return await _context.Vouchers
                .AnyAsync(v => v.TemplateId == templateId && v.RedeemedByUserId == userId);
        }

        public async Task<VoucherEntity?> IssueVoucherAsync(string userId, VoucherTemplate template, object ruleContext)
        {
            if (!RuleEvaluator.Evaluate(template.RuleJson, ruleContext))
                return null;

            if (await HasVoucher(userId, template.Id))
                return null;

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
