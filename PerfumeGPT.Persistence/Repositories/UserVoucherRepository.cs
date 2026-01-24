using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserVoucherRepository : GenericRepository<UserVoucher>, IUserVoucherRepository
	{
		public UserVoucherRepository(PerfumeDbContext context) : base(context)
		{
		}

	public async Task<(List<UserVoucher> Items, int TotalCount)> GetPagedWithVouchersAsync(
		Guid userId,
		GetUserVouchersRequest request)
	{
		var query = _context.UserVouchers
			.Include(uv => uv.Voucher)
			.Where(uv => uv.UserId == userId)
			.AsNoTracking();

		// Apply filters using expression composition
		if (request.Status.HasValue)
		{
			query = query.Where(uv => uv.Status == request.Status.Value);
		}

		if (request.IsUsed.HasValue)
		{
			query = query.Where(uv => uv.IsUsed == request.IsUsed.Value);
		}

		if (request.IsExpired.HasValue)
		{
			var now = DateTime.UtcNow;
			if (request.IsExpired.Value)
			{
				query = query.Where(uv => uv.Voucher != null && uv.Voucher.ExpiryDate < now);
			}
			else
			{
				query = query.Where(uv => uv.Voucher != null && uv.Voucher.ExpiryDate >= now);
			}
		}

		if (!string.IsNullOrEmpty(request.Code))
		{
			// Use CollateContains for case-insensitive search
			var codeFilter = EfCollationExtensions.CollateContains<UserVoucher>(
				uv => uv.Voucher != null ? uv.Voucher.Code : null,
				request.Code);
			query = query.Where(codeFilter);
		}

		if (request.DiscountType.HasValue)
		{
			query = query.Where(uv => uv.Voucher != null && uv.Voucher.DiscountType == request.DiscountType.Value);
		}

		// Get total count before pagination
		var totalCount = await query.CountAsync();

		// Apply sorting using QueryableExtensions
		var sortBy = MapSortFieldToProperty(request.SortBy);
		var sortedQuery = query.ApplySorting(sortBy, request.IsDescending);

		// Apply pagination
		var items = await sortedQuery
			.Skip((request.PageNumber - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToListAsync();

		return (items, totalCount);
	}

	/// <summary>
	/// Maps user-friendly sort field names to actual property paths
	/// </summary>
	private static string? MapSortFieldToProperty(string? sortBy)
	{
		if (string.IsNullOrEmpty(sortBy))
		{
			return "CreatedAt"; // Default sort field
		}

		return sortBy.ToLower() switch
		{
			"createdat" => "CreatedAt",
			"status" => "Status",
			"isused" => "IsUsed",
			"code" => "Voucher.Code",
			"discountvalue" => "Voucher.DiscountValue",
			"expirydate" => "Voucher.ExpiryDate",
			"discounttype" => "Voucher.DiscountType",
			_ => "CreatedAt" // Default fallback
		};
	}

		public async Task<bool> HasRedeemedVoucherAsync(Guid userId, Guid voucherId)
		{
			return await _context.UserVouchers
				.AsNoTracking()
				.AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
		}

		public async Task<UserVoucher?> GetUnusedUserVoucherAsync(Guid userId, Guid voucherId)
		{
			return await _context.UserVouchers
				.AsNoTracking()
				.FirstOrDefaultAsync(uv => 
					uv.UserId == userId && 
					uv.VoucherId == voucherId && 
					!uv.IsUsed &&
					uv.Status == UsageStatus.Available);
		}
	}
}


