namespace VoucherGrpc.Models
{
    public class VoucherTemplate
    {
        public Guid Id { get; set; }
        public string CodePrefix { get; set; } = null!;
        public double DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "fixed";
        public int ValidDays { get; set; }
        public string RuleJson { get; set; } = null!;
        public bool AutoIssue { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VoucherEntity> Vouchers { get; set; } = new List<VoucherEntity>();
        public string DisplayName { get;  set; } = null!; // Added DisplayName for better UX
    }
}
