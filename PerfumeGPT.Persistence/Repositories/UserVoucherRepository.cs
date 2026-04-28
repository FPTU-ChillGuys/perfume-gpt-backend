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
		public UserVoucherRepository(PerfumeDbContext context) : base(context) { }

		public async Task MigrateGuestVouchersAsync(Guid userId, string email, string phoneNumber)
		{
			var guestVouchers = await _context.UserVouchers
				.Where(uv =>
					uv.UserId == null &&
					((!string.IsNullOrEmpty(email) && uv.GuestIdentifier == email) ||
					 (!string.IsNullOrEmpty(phoneNumber) && uv.GuestIdentifier == phoneNumber))
				)
				.ToListAsync();

			foreach (var voucher in guestVouchers)
			{
				voucher.AssignToUser(userId);
				_context.UserVouchers.Update(voucher);
			}
		}

		public async Task<(List<UserVoucherResponse> Items, int TotalCount)> GetPagedWithVouchersAsync(Guid userId, GetPagedUserVouchersRequest request)
		{
			var now = DateTime.UtcNow;
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
				Expression<Func<UserVoucher, bool>> isUsedFilter = request.IsUsed.Value
					   ? uv => uv.Status == UsageStatus.Used
					   : uv => uv.Status != UsageStatus.Used;
				filter = filter == null ? isUsedFilter : filter.AndAlso(isUsedFilter);
			}

			// IsExpired filter
			if (request.IsExpired.HasValue)
			{
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

            var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(UserVoucher.CreatedAt),
				nameof(UserVoucher.Status),
				"Voucher.Code",
				"Voucher.DiscountValue",
				"Voucher.MinOrderValue",
				"Voucher.ExpiryDate"
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			// Apply sorting
			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(uv => uv.CreatedAt);

			// Apply pagination
			var items = await sortedQuery
				 .Skip((request.PageNumber - 1) * request.PageSize)
				 .Take(request.PageSize)
			  .Select(uv => new UserVoucherResponse
			  {
				  Id = uv.Id,
				  VoucherId = uv.VoucherId,
				  Code = uv.Voucher.Code,
				  DiscountValue = uv.Voucher.DiscountValue,
				  DiscountType = uv.Voucher.DiscountType,
				  MinOrderValue = uv.Voucher.MinOrderValue,
				  ExpiryDate = uv.Voucher.ExpiryDate,
				  IsUsed = uv.Status == UsageStatus.Used,
				  Status = uv.Status,
				  IsExpired = uv.Voucher.ExpiryDate < now,
				  RedeemedAt = uv.CreatedAt
			  })
				 .ToListAsync();

			return (items, totalCount);
		}

		public async Task<int> GetUserVoucherUsageCountAsync(Guid? userId, Guid voucherId, string? guestIdentifier)
		{
			// Đổi signature nhận Guid? userId để dùng chung cho cả User và Guest
			var query = _context.UserVouchers
				.AsNoTracking()
				.Where(uv => uv.VoucherId == voucherId);

			if (userId.HasValue)
			{
				query = query.Where(uv => uv.UserId == userId.Value);
			}
			else if (!string.IsNullOrEmpty(guestIdentifier))
			{
				// Chỉ đếm cho Guest nếu thực sự có truyền Email/Phone
				query = query.Where(uv => uv.UserId == null && uv.GuestIdentifier == guestIdentifier);
			}
			else
			{
				// Khách vãng lai vô danh (không user, không phone) thì không có lịch sử để đếm
				return 0;
			}

			return await query.CountAsync();
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
					uv.Status == UsageStatus.Available &&
					!uv.Voucher.IsDeleted &&
					uv.Voucher.ExpiryDate > now);
		}

		public async Task<List<VoucherResponse>> GetAvailableVoucherDetailsByUserIdAsync(Guid userId)
		{
			var now = DateTime.UtcNow;

			return await _context.UserVouchers
				.Where(uv => uv.UserId == userId
					&& uv.Status == UsageStatus.Available
					&& uv.Voucher != null
					&& !uv.Voucher.IsDeleted
					&& uv.Voucher.ExpiryDate >= now)
				.Select(uv => uv.Voucher)
				.GroupBy(v => v.Id)
				.Select(g => g.Select(v => new VoucherResponse
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
				}).First())
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<(List<AvailableVoucherResponse> Items, int TotalCount)> GetPagedAvailableVouchersAsync(Guid userId, GetPagedAvailableVouchersRequest request)
		{
			var now = DateTime.UtcNow;

			var ownedAvailableVouchers = _context.Vouchers
				 .Where(v => !v.IsDeleted
					 && v.ExpiryDate > now
					 && v.UserVouchers.Any(uv => uv.UserId == userId && uv.Status == UsageStatus.Available));

			var publicFreeVouchers = _context.Vouchers
				.Where(v => !v.IsDeleted
					&& v.ExpiryDate > now
					&& v.IsPublic
					&& v.RequiredPoints == 0
					&& (v.RemainingQuantity == null || v.RemainingQuantity > 0)
					&& (!v.MaxUsagePerUser.HasValue ||
						v.UserVouchers.Count(uv => uv.UserId == userId && (uv.Status == UsageStatus.Used || uv.Status == UsageStatus.Reserved)) < v.MaxUsagePerUser.Value));

			var query = ownedAvailableVouchers
				.Union(publicFreeVouchers)
				.Select(v => new AvailableVoucherResponse
				{
					Id = v.Id,
					Code = v.Code,
					DiscountValue = v.DiscountValue,
					DiscountType = v.DiscountType,
					MaxDiscountAmount = v.MaxDiscountAmount,
					MinOrderValue = v.MinOrderValue,
					ExpiryDate = v.ExpiryDate,
					RemainingQuantity = v.RemainingQuantity,
					MaxUsagePerUser = v.MaxUsagePerUser
				})
				.AsNoTracking();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(AvailableVoucherResponse.Code),
				nameof(AvailableVoucherResponse.DiscountValue),
				nameof(AvailableVoucherResponse.MinOrderValue),
				nameof(AvailableVoucherResponse.ExpiryDate),
				nameof(AvailableVoucherResponse.RemainingQuantity),
				nameof(AvailableVoucherResponse.MaxUsagePerUser)
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderBy(v => v.ExpiryDate);

			var totalCount = await query.CountAsync();
         var items = await sortedQuery
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}


