using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationAPI.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        [MaxLength(200)]
        public string? RefreshToken { get; set; }

        public UserRole Role { get; set; } = UserRole.User;

        // Optional enhancements
        public DateTime? BirthDate { get; set; }

        [NotMapped]
        public int Age => BirthDate.HasValue
            ? (int)((DateTime.UtcNow - BirthDate.Value).TotalDays / 365.25)
            : 0;

        [NotMapped]
        public bool IsNew => CreatedAt > DateTime.UtcNow.AddDays(-3);
    }
}
