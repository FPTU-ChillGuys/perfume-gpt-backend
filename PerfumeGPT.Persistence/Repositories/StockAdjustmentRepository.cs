using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StockAdjustmentRepository : GenericRepository<StockAdjustment>, IStockAdjustmentRepository
	{
		public StockAdjustmentRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<StockAdjustment?> GetByIdWithDetailsAsync(Guid id)
		{
			return await _context.StockAdjustments
				.Include(sa => sa.CreatedByUser)
				.Include(sa => sa.VerifiedByUser)
				.Include(sa => sa.AdjustmentDetails)
					.ThenInclude(d => d.ProductVariant)
						.ThenInclude(v => v.Product)
				.Include(sa => sa.AdjustmentDetails)
					.ThenInclude(d => d.Batch)
				.AsNoTracking()
				.FirstOrDefaultAsync(sa => sa.Id == id);
		}

		public async Task<StockAdjustment?> GetByIdWithDetailsForDeleteAsync(Guid id)
		{
			return await _context.StockAdjustments
				.Include(sa => sa.AdjustmentDetails)
				.FirstOrDefaultAsync(sa => sa.Id == id);
		}

		public async Task<(IEnumerable<StockAdjustment> Items, int TotalCount)> GetPagedWithDetailsAsync(GetPagedStockAdjustmentsRequest request)
		{
			var query = _context.StockAdjustments
				.Include(sa => sa.CreatedByUser)
				.Include(sa => sa.AdjustmentDetails)
				.AsNoTracking()
				.AsQueryable();

			// Apply filters
			if (request.Reason.HasValue)
				query = query.Where(sa => sa.Reason == request.Reason.Value);

			if (request.Status.HasValue)
				query = query.Where(sa => sa.Status == request.Status.Value);

			if (request.FromDate.HasValue)
				query = query.Where(sa => sa.AdjustmentDate >= request.FromDate.Value);

			if (request.ToDate.HasValue)
				query = query.Where(sa => sa.AdjustmentDate <= request.ToDate.Value);

			// Get total count
			var totalCount = await query.CountAsync();

			// Apply ordering
			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(sa => sa.AdjustmentDate)
				: query.OrderByDescending(sa => sa.AdjustmentDate);

			// Apply paging
			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
