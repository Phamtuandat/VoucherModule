

using Contract.Abstracts;

namespace Contract.VoucherEvents
{
    public record VoucherIssuedEvent(Guid? UserId, string VoucherCode = "", DateTime CreatedAt = default) : IntegrationEvent;
    
}
