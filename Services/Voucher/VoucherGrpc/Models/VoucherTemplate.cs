namespace VoucherGrpc.Models
{
    public class VoucherTemplate
    {
        public string Description { get; set; }
        public Guid Id { get; set; }
        public string CodePrefix { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public DiscountType DiscountType { get; set; }
        public int ValidDays { get; set; }
        public string RuleJson { get; set; } = null!;
        public bool AutoIssue { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; }
        public ICollection<VoucherEntity> Vouchers { get; set; } = new List<VoucherEntity>();
        public string DisplayName { get;  set; } = null!; // Added DisplayName for better UX
    }
}
