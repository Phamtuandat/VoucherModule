namespace VoucherGrpc.Services
{
    public interface IVoucherService
    {
        Task<VoucherEntity?> IssueVoucherAsync(string userId, VoucherTemplate template, object ruleContext);
        Task<bool> HasVoucher(string userId, Guid templateId);
        Task<VoucherEntity?> RedeemVoucherAsync(string code, string userId);
        Task<IEnumerable<VoucherEntity>> GetVouchersByUser(string userId);
    }
}
