using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserVoucherRepository : GenericRepository<UserVoucher>, IUserVoucherRepository
	{
		public UserVoucherRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task MigrateGuestVouchersAsync(Guid userId, string email, string phoneNumber)
		{
			var guestVouchers = await _context.UserVouchers
				.Where(uv =>
					uv.UserId == null &&
					((!string.IsNullOrEmpty(email) && uv.GuestEmailOrPhone == email) ||
					 (!string.IsNullOrEmpty(phoneNumber) && uv.GuestEmailOrPhone == phoneNumber))
				)
				.ToListAsync();

			foreach (var voucher in guestVouchers)
			{
				voucher.UserId = userId;
			}

			await _context.SaveChangesAsync();
		}

		public async Task<(List<UserVoucher> Items, int TotalCount)> GetPagedWithVouchersAsync(Guid userId, GetPagedUserVouchersRequest request)
		{
			var query = _context.UserVouchers
				.Include(uv => uv.Voucher)
				.Where(uv => uv.UserId == userId)
				.AsNoTracking();

			// Build filter expression using composition
			Expression<Func<UserVoucher, bool>>? filter = null;

			// Status filter
			if (request.Status.HasValue)
			{
				Expression<Func<UserVoucher, bool>> statusFilter = uv => uv.Status == request.Status.Value;
				filter = filter == null ? statusFilter : filter.AndAlso(statusFilter);
			}

			// IsUsed filter
			if (request.IsUsed.HasValue)
			{
				Expression<Func<UserVoucher, bool>> isUsedFilter = uv => uv.IsUsed == request.IsUsed.Value;
				filter = filter == null ? isUsedFilter : filter.AndAlso(isUsedFilter);
			}

			// IsExpired filter
			if (request.IsExpired.HasValue)
			{
				var now = DateTime.UtcNow;
				Expression<Func<UserVoucher, bool>> expiredFilter;

				if (request.IsExpired.Value)
				{
					expiredFilter = uv => uv.Voucher != null && uv.Voucher.ExpiryDate < now;
				}
				else
				{
					expiredFilter = uv => uv.Voucher != null && uv.Voucher.ExpiryDate >= now;
				}

				filter = filter == null ? expiredFilter : filter.AndAlso(expiredFilter);
			}

			// Code search filter
			if (!string.IsNullOrEmpty(request.Code))
			{
				var codeFilter = EfCollationExtensions.CollateContains<UserVoucher>(
					uv => uv.Voucher != null ? uv.Voucher.Code : null,
					request.Code);

				filter = filter == null ? codeFilter : filter.AndAlso(codeFilter);
			}

			// DiscountType filter
			if (request.DiscountType.HasValue)
			{
				Expression<Func<UserVoucher, bool>> discountTypeFilter =
					uv => uv.Voucher != null && uv.Voucher.DiscountType == request.DiscountType.Value;

				filter = filter == null ? discountTypeFilter : filter.AndAlso(discountTypeFilter);
			}

			// Apply combined filter
			if (filter != null)
			{
				query = query.Where(filter);
			}

			// Get total count before pagination
			var totalCount = await query.CountAsync();

			// Apply sorting
			var sortedQuery = query.ApplySorting(request.SortBy, request.IsDescending);

			// Apply pagination
			var items = await sortedQuery
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<bool> HasRedeemedVoucherAsync(Guid userId, Guid voucherId, string? guestEmailOrPhone)
		{
			return await _context.UserVouchers
				.AsNoTracking()
				.AnyAsync(uv =>
					(uv.UserId == userId && uv.VoucherId == voucherId) ||
					(uv.GuestEmailOrPhone == guestEmailOrPhone && uv.VoucherId == voucherId)
				);
		}

		public async Task<UserVoucher?> GetUnusedUserVoucherAsync(Guid userId, Guid voucherId)
		{
			var now = DateTime.UtcNow;
			return await _context.UserVouchers
				.Include(uv => uv.Voucher)
				.AsNoTracking()
				.FirstOrDefaultAsync(uv =>
					uv.UserId == userId &&
					uv.VoucherId == voucherId &&
					!uv.IsUsed &&
					uv.Status == UsageStatus.Available &&
					!uv.Voucher.IsDeleted);
		}

		public async Task<(List<AvailableVoucherResponse> Items, int TotalCount)> GetPagedAvailableVouchersAsync(Guid userId, GetPagedAvailableVouchersRequest request)
		{
			var query = _context.UserVouchers
				.Where(uv =>
					(uv.UserId == userId && uv.Status == UsageStatus.Available && !uv.IsUsed)
					||
					(uv.Voucher.IsPublic
						&& uv.Voucher.ExpiryDate > DateTime.Now
						&& !uv.Voucher.IsDeleted
						&& uv.Voucher.RemainingQuantity > 0
						&& !_context.UserVouchers.Any(used =>
							used.UserId == userId && used.VoucherId == uv.VoucherId)))
				.ProjectToType<AvailableVoucherResponse>()
				.AsNoTracking();

			var totalCount = await query.CountAsync();
			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}


