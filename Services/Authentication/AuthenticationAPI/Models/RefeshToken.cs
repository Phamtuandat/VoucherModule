namespace AuthenticationAPI.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }

        public User? User { get; set; }
    }

}
