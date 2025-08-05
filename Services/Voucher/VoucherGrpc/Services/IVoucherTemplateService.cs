namespace VoucherGrpc.Services
{
    public interface IVoucherTemplateService
    {
        Task<VoucherTemplate?> GetTemplateByPrefixAsync(string code, CancellationToken cancellationToken = default);
        Task<List<VoucherTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
        Task<VoucherTemplate> CreateTemplateAsync(VoucherTemplate template, CancellationToken cancellationToken = default);
        Task<VoucherTemplate?> UpdateTemplateAsync(VoucherTemplate template, CancellationToken cancellationToken = default);
        Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    }
}
