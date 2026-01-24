using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VoucherRepository : GenericRepository<Voucher>, IVoucherRepository
	{
		public VoucherRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null)
		{
			var query = _context.Vouchers
				.Where(v => v.Code == code.ToUpper() && !v.IsDeleted);

			if (excludeVoucherId.HasValue)
			{
				query = query.Where(v => v.Id != excludeVoucherId.Value);
			}

			return await query.AnyAsync();
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVouchersAsync(GetPagedVouchersRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Vouchers
				.Where(v => !v.IsDeleted)
				.AsNoTracking();

			// Filter by expiration status
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

			// Filter by code
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


