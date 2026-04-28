using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class InventoryLedgerRepository : GenericRepository<InventoryLedger>, IInventoryLedgerRepository
	{
		public InventoryLedgerRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<InventoryLedgerItemResponse> Items, int TotalCount)> GetPagedAsync(GetInventoryLedgersRequest request)
		{
			IQueryable<InventoryLedger> query = _context.Set<InventoryLedger>().AsNoTracking();
			Expression<Func<InventoryLedger, bool>> filter = x => true;

			if (request.FromDate.HasValue)
			{
				var fromDate = request.FromDate.Value.ToUniversalTime();
				filter = filter.AndAlso(x => x.CreatedAt >= fromDate);
			}

			if (request.ToDate.HasValue)
			{
				var toDate = request.ToDate.Value.ToUniversalTime();
				filter = filter.AndAlso(x => x.CreatedAt <= toDate);
			}

			if (request.VariantId.HasValue)
			{
				var variantId = request.VariantId.Value;
				filter = filter.AndAlso(x => x.VariantId == variantId);
			}

			if (request.BatchId.HasValue)
			{
				var batchId = request.BatchId.Value;
				filter = filter.AndAlso(x => x.BatchId == batchId);
			}

			if (request.Type.HasValue)
			{
				var type = request.Type.Value;
				filter = filter.AndAlso(x => x.Type == type);
			}

			if (request.ReferenceId.HasValue)
			{
				var referenceId = request.ReferenceId.Value;
				filter = filter.AndAlso(x => x.ReferenceId == referenceId);
			}

			if (request.ActorId.HasValue)
			{
				var actorId = request.ActorId.Value;
				filter = filter.AndAlso(x => x.ActorId == actorId);
			}

			query = query.Where(filter);
			var totalCount = await query.CountAsync();
			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(InventoryLedger.CreatedAt),
				nameof(InventoryLedger.QuantityChange),
				nameof(InventoryLedger.BalanceAfter),
				nameof(InventoryLedger.Type)
			};
			var sortBy = request.SortBy?.Trim();
			sortBy = !string.IsNullOrWhiteSpace(sortBy)
				? (sortBy.Length == 1
					? char.ToUpper(sortBy[0]).ToString()
					: char.ToUpper(sortBy[0]) + sortBy.Substring(1))
				: null;
			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(x => x.CreatedAt);

			var items = await sortedQuery
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
