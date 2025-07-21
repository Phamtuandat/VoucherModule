namespace VoucherGrpc.Services
{
    public class VoucherServiceImpl : Voucher.VoucherBase
    {
        private readonly VoucherDbContext _context;
        public VoucherServiceImpl(VoucherDbContext context) => _context = context;

        public override async Task<VoucherResponse> CreateVoucher(CreateVoucherRequest request, ServerCallContext context)
        {
            var voucher = new VoucherEntity { Code = request.Code, Amount = request.Amount };
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = voucher.Amount,
                IsRedeemed = voucher.IsRedeemed,
                Message = "Voucher created"
            };
        }

        public override async Task<VoucherResponse> RedeemVoucher(RedeemVoucherRequest request, ServerCallContext context)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == request.Code);
            if (voucher == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Voucher not found"));

            if (voucher.IsRedeemed)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Voucher already redeemed"));

            voucher.IsRedeemed = true;
            await _context.SaveChangesAsync();

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = voucher.Amount,
                IsRedeemed = voucher.IsRedeemed,
                Message = "Voucher redeemed"
            };
        }

        public override async Task<VoucherResponse> GetVoucherStatus(VoucherStatusRequest request, ServerCallContext context)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == request.Code);
            if (voucher == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Voucher not found"));

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = voucher.Amount,
                IsRedeemed = voucher.IsRedeemed,
                Message = "Voucher status retrieved"
            };
        }
        [Authorize(Policy = "VoucherRead")]
        public override async Task<VoucherListResponse> GetAllVouchers(Empty request, ServerCallContext context)
        {
            var vouchers = await _context.Vouchers.ToListAsync();

            var response = new VoucherListResponse();

            foreach (var voucher in vouchers)
            {
                response.Vouchers.Add(new VoucherResponse
                {
                    Code = voucher.Code,
                    Amount = voucher.Amount,
                    IsRedeemed = voucher.IsRedeemed
                });
            }

            return response;
        }

        public override async Task<MigrationResponse> MigrateDatabase(Empty request, ServerCallContext context)
        {
            try
            {
                // 1. Apply pending migrations
                await _context.Database.MigrateAsync();

                // 2. Seed data if necessary
                if (!_context.Vouchers.Any())
                {
                    await DbSeeder.SeedAsync(_context);
                }

                return new MigrationResponse
                {
                    Success = true,
                    Message = "Migration and seeding completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new MigrationResponse
                {
                    Success = false,
                    Message = $"Migration failed: {ex.Message}"
                };
            }
        }

    }
}
