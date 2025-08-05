
using Microsoft.EntityFrameworkCore;

namespace VoucherGrpc.Services
{
    public class VoucherTemplateService(VoucherDbContext context, ILogger<VoucherTemplateService> logger) : IVoucherTemplateService
    {
        private readonly ILogger<VoucherTemplateService> _logger = logger;
        private readonly VoucherDbContext _context = context;

        public async Task<VoucherTemplate> CreateTemplateAsync(VoucherTemplate template, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template.CodePrefix) || template.ValidDays <= 0 || template.DiscountAmount <= 0)
            {
                throw new ArgumentException("Invalid template properties");
            }

            await _context.VoucherTemplates.AddAsync(template, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // ✅ required

            _logger.LogInformation("✅ Created voucher template with ID {TemplateId}", template.Id);

            return template;
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await _context.VoucherTemplates.FindAsync([templateId], cancellationToken); 

                if (template == null)
                {
                    throw new KeyNotFoundException("Template not found");
                }
                await _context.VoucherTemplates
                    .Where(t => t.Id == templateId)
                    .ExecuteDeleteAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                // Fix: Ensure the number of placeholders matches the number of parameters
                _logger.LogError("Error deleting template with ID {TemplateId}: {ErrorMessage}", templateId, ex.Message);
                return false;
            }
        }

        public async Task<List<VoucherTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.VoucherTemplates
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<VoucherTemplate?> GetTemplateByPrefixAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.VoucherTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.CodePrefix == code, cancellationToken);
        }

        public async Task<VoucherTemplate?> UpdateTemplateAsync(VoucherTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.VoucherTemplates
                    .Where(vt => vt.Id == template.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(vt => vt.CodePrefix, vt => template.CodePrefix)
                        .SetProperty(vt => vt.ValidDays, vt => template.ValidDays)
                        .SetProperty(vt => vt.DiscountAmount, vt => template.DiscountAmount)
                        .SetProperty(vt => vt.DiscountType, vt => template.DiscountType)
                        .SetProperty(vt => vt.ExpiryDate, vt => template.ExpiryDate)
                        .SetProperty(vt => vt.Description, vt => template.Description)
                        .SetProperty(vt => vt.RuleJson, vt => template.RuleJson)
                        .SetProperty(vt => vt.DisplayName, vt => template.DisplayName)
                        .SetProperty(vt => vt.AutoIssue, vt => template.AutoIssue),
                    cancellationToken);

                return template;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error updating template with ID {TemplateId}: {ErrorMessage}", template.Id, ex.Message);
                return null;
            }
        }
    }
}
