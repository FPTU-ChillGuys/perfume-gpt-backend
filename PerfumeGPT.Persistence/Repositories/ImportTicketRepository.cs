using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ImportTicketRepository : GenericRepository<ImportTicket>, IImportTicketRepository
	{
		public ImportTicketRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<ImportTicket?> GetByIdWithDetailsAsync(Guid id)
		{
			return await _context.ImportTickets
				.Include(it => it.CreatedByUser)
				.Include(it => it.VerifiedByUser)
				.Include(it => it.Supplier)
				.Include(it => it.ImportDetails)
					.ThenInclude(d => d.ProductVariant)
						.ThenInclude(v => v.Product)
				.Include(it => it.ImportDetails)
					.ThenInclude(d => d.Batches)
				.AsNoTracking()
				.FirstOrDefaultAsync(it => it.Id == id);
		}

		public async Task<ImportTicket?> GetByIdWithDetailsForDeleteAsync(Guid id)
		{
			return await _context.ImportTickets
				.Include(it => it.ImportDetails)
					.ThenInclude(d => d.Batches)
				.FirstOrDefaultAsync(it => it.Id == id);
		}

		public async Task<(IEnumerable<ImportTicket> Items, int TotalCount)> GetPagedWithDetailsAsync(GetPagedImportTicketsRequest request)
		{
			var query = _context.ImportTickets
				.Include(it => it.CreatedByUser)
				.Include(it => it.VerifiedByUser)
				.Include(it => it.Supplier)
				.Include(it => it.ImportDetails)
				.AsNoTracking()
				.AsQueryable();

			// Apply filters
			if (request.SupplierId.HasValue)
				query = query.Where(it => it.SupplierId == request.SupplierId.Value);

			if (request.Status.HasValue)
				query = query.Where(it => it.Status == request.Status.Value);

			if (request.FromDate.HasValue)
				query = query.Where(it => it.ExpectedArrivalDate >= request.FromDate.Value);

			if (request.ToDate.HasValue)
				query = query.Where(it => it.ExpectedArrivalDate <= request.ToDate.Value);

			if (request.VerifiedById.HasValue)
				query = query.Where(it => it.VerifiedById == request.VerifiedById.Value);

			// Get total count
			var totalCount = await query.CountAsync();

			// Apply ordering
			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(it => it.ExpectedArrivalDate)
				: query.OrderByDescending(it => it.ExpectedArrivalDate);

			// Apply paging
			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
