using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
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
				   IsMemberOnly = v.IsMemberOnly,
				   CreatedAt = v.CreatedAt
			   })
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<VoucherResponse?> GetByIdResponseAsync(Guid voucherId)
		{
			var now = DateTime.UtcNow;

			return await _context.Vouchers
				.Where(v => v.Id == voucherId && !v.IsDeleted)
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
					IsExpired = v.ExpiryDate < now,
					TotalQuantity = v.TotalQuantity,
					RemainingQuantity = v.RemainingQuantity,
					MaxUsagePerUser = v.MaxUsagePerUser,
					IsPublic = v.IsPublic,
					IsMemberOnly = v.IsMemberOnly,
					CreatedAt = v.CreatedAt
				})
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<List<VoucherResponse>> GetByCampaignIdAsync(Guid campaignId)
			=> await _context.Vouchers
				.Where(v => v.CampaignId == campaignId && !v.IsDeleted)
				.OrderByDescending(v => v.CreatedAt)
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
					IsMemberOnly = v.IsMemberOnly,
					CreatedAt = v.CreatedAt
				})
				.AsNoTracking()
				.ToListAsync();

		// Thêm vào IVoucherRepository và VoucherRepository
		public async Task<List<VoucherResponse>> GetPublicVouchersForVariantAsync(Guid variantId)
		{
			var now = DateTime.UtcNow;

			return await _context.Vouchers
				.Where(v =>
					!v.IsDeleted
					&& v.IsPublic
					&& v.RequiredPoints == 0
					&& v.ExpiryDate >= now
					&& (!v.RemainingQuantity.HasValue || v.RemainingQuantity.Value > 0)
					&& (
						// 1. Lấy Voucher toàn đơn (vì mua sản phẩm này xong gộp vào đơn vẫn được giảm)
						v.ApplyType == VoucherType.Order
						||
						// 2. Lấy Voucher theo sản phẩm NHƯNG bắt buộc chiến dịch phải chứa Variant này
						(v.ApplyType == VoucherType.Product
						 && v.CampaignId.HasValue
						 && _context.Promotions.Any(p =>
								p.CampaignId == v.CampaignId
								&& p.TargetProductVariantId == variantId
								&& p.ItemType == v.TargetItemType
								&& !p.IsDeleted
								&& p.IsActive))
					))
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
					IsExpired = false, // Đã lọc ở trên
					TotalQuantity = v.TotalQuantity,
					RemainingQuantity = v.RemainingQuantity,
					MaxUsagePerUser = v.MaxUsagePerUser,
					IsPublic = v.IsPublic,
					IsMemberOnly = v.IsMemberOnly,
					CreatedAt = v.CreatedAt
				})
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<VoucherResponse>> GetPublicVouchersForApplicabilityAsync()
		{
			var now = DateTime.UtcNow;

			return await _context.Vouchers
				.Where(v =>
					!v.IsDeleted
					&& v.IsPublic
					&& v.ExpiryDate >= now
					&& (!v.RemainingQuantity.HasValue || v.RemainingQuantity.Value > 0)
					&& (v.CampaignId.HasValue || v.RequiredPoints == 0))
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
					IsExpired = v.ExpiryDate < now,
					TotalQuantity = v.TotalQuantity,
					RemainingQuantity = v.RemainingQuantity,
					MaxUsagePerUser = v.MaxUsagePerUser,
					IsPublic = v.IsPublic,
					IsMemberOnly = v.IsMemberOnly,
					CreatedAt = v.CreatedAt
				})
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<(List<RedeemableVoucherResponse> Items, int TotalCount)> GetPagedRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request, Guid? userId = null)
		{
			var now = DateTime.UtcNow;
			var query = _context.Vouchers
			   .Where(v =>
					!v.IsDeleted
					&& v.CampaignId == null
					&& v.ExpiryDate >= now
					&& v.IsPublic
					&& v.RequiredPoints > 0
					&& v.RemainingQuantity > 0
					&& (!userId.HasValue
						|| !v.MaxUsagePerUser.HasValue
						|| _context.UserVouchers.Count(uv => uv.UserId == userId.Value && uv.VoucherId == v.Id) < v.MaxUsagePerUser.Value))
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
				 IsExpired = v.ExpiryDate < now,
				 RemainingQuantity = v.RemainingQuantity,
				 MaxUsagePerUser = v.MaxUsagePerUser,
				 CreatedAt = v.CreatedAt
			 })
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<VoucherResponse> Items, int TotalCount)> GetPagedVouchersAsync(GetPagedVouchersRequest request)
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
					IsExpired = v.ExpiryDate < now,
					TotalQuantity = v.TotalQuantity,
					RemainingQuantity = v.RemainingQuantity,
					MaxUsagePerUser = v.MaxUsagePerUser,
					IsPublic = v.IsPublic,
					IsMemberOnly = v.IsMemberOnly,
					CreatedAt = v.CreatedAt
				})
				   .ToListAsync();

			return (items, totalCount);
		}
	}
}


