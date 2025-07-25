using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoucherGrpc.Models
{
    public class VoucherEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Range(0.01, (double)decimal.MaxValue)] // Fix: Cast decimal.MaxValue to double
        public decimal Amount { get; set; }

        public bool IsPublic { get; set; }

        public bool IsRedeemed { get; set; }

        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RedeemedAt { get; set; }

        [MaxLength(100)]
        public string? RedeemedByUserId { get; set; }  // Optional: who used it

        public Guid TemplateId { get; set; }

        [ForeignKey(nameof(TemplateId))]
        public VoucherTemplate Template { get; set; } = null!;
    }
}
