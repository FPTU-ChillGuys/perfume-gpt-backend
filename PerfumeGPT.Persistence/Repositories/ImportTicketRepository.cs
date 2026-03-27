using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ImportTicketRepository : GenericRepository<ImportTicket>, IImportTicketRepository
	{
		public ImportTicketRepository(PerfumeDbContext context) : base(context) { }

		public async Task<ImportTicket?> GetByIdWithDetailsAsync(Guid id)
			=> await _context.ImportTickets
				.AsNoTracking()
				.Include(it => it.ImportDetails)
				.FirstOrDefaultAsync(it => it.Id == id);

		public async Task<ImportTicketResponse?> GetResponseByIdAsync(Guid id)
			=> await _context.ImportTickets
				.AsNoTracking()
				.AsSplitQuery()
				.Where(it => it.Id == id)
				.ProjectToType<ImportTicketResponse>()
				.FirstOrDefaultAsync();

		public async Task<ImportTicket?> GetByIdWithDetailsAndBatchesAsync(Guid id)
			=> await _context.ImportTickets
				.Include(it => it.ImportDetails)
					.ThenInclude(d => d.Batches)
				.FirstOrDefaultAsync(it => it.Id == id);

		public async Task<(List<ImportTicketListItem> Items, int TotalCount)> GetPagedAsync(GetPagedImportTicketsRequest request)
		{
			var query = _context.ImportTickets.AsNoTracking().AsQueryable();

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

			var totalCount = await query.CountAsync();

			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(it => it.ExpectedArrivalDate)
				: query.OrderByDescending(it => it.ExpectedArrivalDate);

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.AsSplitQuery()
				.ProjectToType<ImportTicketListItem>()
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
