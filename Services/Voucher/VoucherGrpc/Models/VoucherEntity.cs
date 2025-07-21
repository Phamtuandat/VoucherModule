namespace VoucherGrpc.Models
{
    public class VoucherEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public double Amount { get; set; }
        public bool IsRedeemed { get; set; }
        public DateTime ExpiryDate { get; set; }

        public Guid TemplateId { get; set; } 
        public VoucherTemplate Template { get; set; } = null!; 
    }

}
