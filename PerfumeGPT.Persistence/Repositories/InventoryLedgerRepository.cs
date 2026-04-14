using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class InventoryLedgerRepository : GenericRepository<InventoryLedger>, IInventoryLedgerRepository
	{
		public InventoryLedgerRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<InventoryLedgerItemResponse> Items, int TotalCount)> GetPagedAsync(GetInventoryLedgersRequest request)
		{
			IQueryable<InventoryLedger> query = _context.Set<InventoryLedger>().AsNoTracking();

			if (request.FromDate.HasValue)
			{
				var fromDate = request.FromDate.Value.ToUniversalTime();
				query = query.Where(x => x.CreatedAt >= fromDate);
			}

			if (request.ToDate.HasValue)
			{
				var toDate = request.ToDate.Value.ToUniversalTime();
				query = query.Where(x => x.CreatedAt <= toDate);
			}

			if (request.VariantId.HasValue)
			{
				query = query.Where(x => x.VariantId == request.VariantId.Value);
			}

			if (request.BatchId.HasValue)
			{
				query = query.Where(x => x.BatchId == request.BatchId.Value);
			}

			if (request.Type.HasValue)
			{
				query = query.Where(x => x.Type == request.Type.Value);
			}

			if (request.ReferenceId.HasValue)
			{
				query = query.Where(x => x.ReferenceId == request.ReferenceId.Value);
			}

			if (request.ActorId.HasValue)
			{
				query = query.Where(x => x.ActorId == request.ActorId.Value);
			}

			var totalCount = await query.CountAsync();
			var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(InventoryLedger.CreatedAt) : request.SortBy;

			var items = await query
				.ApplySorting(sortBy, request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new InventoryLedgerItemResponse
				{
					Id = x.Id,
					CreatedAt = x.CreatedAt,
					VariantId = x.VariantId,
					BatchId = x.BatchId,
					QuantityChange = x.QuantityChange,
					BalanceAfter = x.BalanceAfter,
					Type = x.Type,
					ReferenceId = x.ReferenceId,
					Description = x.Description,
					ActorId = x.ActorId
				})
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
