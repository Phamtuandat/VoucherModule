using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using VoucherGrpc.Models;
using VoucherGrpc.Data;
using VoucherGrpc;
using Microsoft.AspNetCore.Authorization;

namespace VoucherGrpc.Services
{
    public class VoucherServiceImpl : Voucher.VoucherBase
    {
        private readonly VoucherDbContext _context;

        public VoucherServiceImpl(VoucherDbContext context)
        {
            _context = context;
        }

        public override async Task<VoucherResponse> CreateVoucher(CreateVoucherRequest request, ServerCallContext context)
        {
            // Validate template
            var template = await _context.VoucherTemplates
                .FirstOrDefaultAsync(t => t.Id.ToString() == request.TemplateId);

            if (template == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Template not found"));

            // Check if user already received this voucher
            var exists = await _context.Vouchers
                .AnyAsync(v => v.TemplateId == template.Id && v.RedeemedByUserId == request.UserId);

            if (exists)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "User already has this voucher"));

            var voucher = new VoucherEntity
            {
                Code = $"VC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                Amount = template.DiscountAmount,
                TemplateId = template.Id,
                RedeemedByUserId = request.UserId,
                ExpiryDate = template.ExpiryDate,
                IsRedeemed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = (double)voucher.Amount,
                IsRedeemed = voucher.IsRedeemed,
                ExpiryDate = voucher.ExpiryDate.ToString("o"),
                UserId = voucher.RedeemedByUserId,
                TemplateId = voucher.TemplateId.ToString(),
                Message = "Voucher created successfully"
            };
        }

        public override async Task<VoucherResponse> RedeemVoucher(RedeemVoucherRequest request, ServerCallContext context)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == request.Code && v.RedeemedByUserId == request.UserId);

            if (voucher == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Voucher not found for user"));

            if (voucher.IsRedeemed)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Voucher already redeemed"));

            if (voucher.ExpiryDate < DateTime.UtcNow)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "Voucher is expired"));

            voucher.IsRedeemed = true;
            voucher.RedeemedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = (double)voucher.Amount,
                IsRedeemed = true,
                ExpiryDate = voucher.ExpiryDate.ToString("o"),
                UserId = voucher.RedeemedByUserId,
                TemplateId = voucher.TemplateId.ToString(),
                Message = "Voucher redeemed successfully"
            };
        }

        public override async Task<VoucherResponse> GetVoucherStatus(VoucherStatusRequest request, ServerCallContext context)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Template)
                .FirstOrDefaultAsync(v => v.Code == request.Code);

            if (voucher == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Voucher not found"));

            return new VoucherResponse
            {
                Code = voucher.Code,
                Amount = (double)voucher.Amount,
                IsRedeemed = voucher.IsRedeemed,
                ExpiryDate = voucher.ExpiryDate.ToString("o"),
                UserId = voucher.RedeemedByUserId,
                TemplateId = voucher.TemplateId.ToString(),
                Message = "Voucher status retrieved"
            };
        }

        [Authorize(Policy = "VoucherRead")]
        public override async Task<VoucherListResponse> GetAllVouchers(UserRequest request, ServerCallContext context)
        {
            var vouchers = await _context.Vouchers
                .Where(v => v.RedeemedByUserId == request.UserId)
                .ToListAsync();

            var response = new VoucherListResponse();
            foreach (var voucher in vouchers)
            {
                response.Vouchers.Add(new VoucherResponse
                {
                    Code = voucher.Code,
                    Amount = (double)voucher.Amount,
                    IsRedeemed = voucher.IsRedeemed,
                    ExpiryDate = voucher.ExpiryDate.ToString("o"),
                    UserId = voucher.RedeemedByUserId,
                    TemplateId = voucher.TemplateId.ToString(),
                    Message = "Fetched successfully"
                });
            }

            return response;
        }

        public override async Task<MigrationResponse> MigrateDatabase(Empty request, ServerCallContext context)
        {
            try
            {
                await _context.Database.MigrateAsync();

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
