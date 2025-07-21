
namespace VoucherGrpc.Data
{
    public class VoucherDbContext : DbContext
    {
        public VoucherDbContext(DbContextOptions<VoucherDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VoucherEntity>()
                        .ToTable("Vouchers")
                        .HasOne(v => v.Template)
                        .WithMany(t => t.Vouchers)
                        .HasForeignKey(v => v.TemplateId);

            modelBuilder.Entity<VoucherTemplate>()
                        .ToTable("VoucherTemplates");
        }

        public DbSet<VoucherEntity> Vouchers => Set<VoucherEntity>();
        public DbSet<VoucherTemplate> VoucherTemplates => Set<VoucherTemplate>();
    }
}
