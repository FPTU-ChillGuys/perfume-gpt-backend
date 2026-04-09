using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VoucherRepository : GenericRepository<Voucher>, IVoucherRepository
	{
		public VoucherRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null)
		{
			var normalizedCode = code.Trim().ToLower();

			var query = _context.Vouchers
				.Where(v => v.Code.ToLower() == normalizedCode && !v.IsDeleted);

			if (excludeVoucherId.HasValue)
			{
				query = query.Where(v => v.Id != excludeVoucherId.Value);
			}

			return await query.AnyAsync();
		}

		public async Task<VoucherResponse?> GetByCodeAsync(string code)
		=> await _context.Vouchers
				.Where(v => v.Code.ToLower() == code.ToLower() && !v.IsDeleted)
			   .Select(v => new VoucherResponse
			   {
				   Id = v.Id,
				   Code = v.Code,
				   DiscountValue = v.DiscountValue,
				   DiscountType = v.DiscountType,
				   CampaignId = v.CampaignId,
				   ApplyType = v.ApplyType,
				   TargetItemType = v.TargetItemType ?? default,
				   RequiredPoints = v.RequiredPoints,
                MaxDiscountAmount = v.MaxDiscountAmount,
				   MinOrderValue = v.MinOrderValue,
				   ExpiryDate = v.ExpiryDate,
				   IsExpired = v.ExpiryDate < DateTime.UtcNow,
				   TotalQuantity = v.TotalQuantity,
				   RemainingQuantity = v.RemainingQuantity,
                  MaxUsagePerUser = v.MaxUsagePerUser,
				   IsPublic = v.IsPublic,
				   CreatedAt = v.CreatedAt
			   })
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<(List<RedeemableVoucherResponse> Items, int TotalCount)> GetPagedRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request)
		{
			var query = _context.Vouchers
				.Where(v => !v.IsDeleted && v.ExpiryDate >= DateTime.UtcNow && v.IsPublic && v.RequiredPoints > 0 && v.RemainingQuantity > 0)
				.AsNoTracking();

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
			 .Select(v => new RedeemableVoucherResponse
			 {
				 Id = v.Id,
				 Code = v.Code,
				 DiscountValue = v.DiscountValue,
				 DiscountType = v.DiscountType,
				 RequiredPoints = v.RequiredPoints,
                MaxDiscountAmount = v.MaxDiscountAmount,
				 MinOrderValue = v.MinOrderValue,
				 ExpiryDate = v.ExpiryDate,
				 IsExpired = v.ExpiryDate < DateTime.UtcNow,
				 RemainingQuantity = v.RemainingQuantity,
                 MaxUsagePerUser = v.MaxUsagePerUser,
				 CreatedAt = v.CreatedAt
			 })
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVouchersAsync(GetPagedVouchersRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Vouchers
				.Where(v => !v.IsDeleted)
				.AsNoTracking();

			if (request.IsExpired.HasValue)
			{
				if (request.IsExpired.Value)
				{
					query = query.Where(v => v.ExpiryDate < now);
				}
				else
				{
					query = query.Where(v => v.ExpiryDate >= now);
				}
			}

			if (!string.IsNullOrEmpty(request.Code))
			{
				query = query.Where(v => v.Code.Contains(request.Code));
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}


