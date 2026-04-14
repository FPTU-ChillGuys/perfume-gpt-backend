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
	public class CashFlowLedgerRepository : GenericRepository<CashFlowLedger>, ICashFlowLedgerRepository
	{
		public CashFlowLedgerRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<CashFlowLedgerItemResponse> Items, int TotalCount)> GetPagedAsync(GetCashFlowLedgersRequest request)
		{
			IQueryable<CashFlowLedger> query = _context.CashFlowLedgers.AsNoTracking();

			if (request.FromDate.HasValue)
			{
				var fromDate = request.FromDate.Value.ToUniversalTime();
				query = query.Where(x => x.TransactionDate >= fromDate);
			}

			if (request.ToDate.HasValue)
			{
				var toDate = request.ToDate.Value.ToUniversalTime();
				query = query.Where(x => x.TransactionDate <= toDate);
			}

			if (request.FlowType.HasValue)
			{
				query = query.Where(x => x.FlowType == request.FlowType.Value);
			}

			if (request.Category.HasValue)
			{
				query = query.Where(x => x.Category == request.Category.Value);
			}

			if (request.ReferenceId.HasValue)
			{
				query = query.Where(x => x.ReferenceId == request.ReferenceId.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.ReferenceCode))
			{
				var refCode = request.ReferenceCode.Trim();
				query = query.Where(x => x.ReferenceCode != null && x.ReferenceCode.Contains(refCode));
			}

			var totalCount = await query.CountAsync();
			var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(CashFlowLedger.TransactionDate) : request.SortBy;

			var items = await query
				.ApplySorting(sortBy, request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new CashFlowLedgerItemResponse
				{
					Id = x.Id,
					TransactionDate = x.TransactionDate,
					Amount = x.Amount,
					FlowType = x.FlowType,
					Category = x.Category,
					ReferenceId = x.ReferenceId,
					ReferenceCode = x.ReferenceCode,
					Description = x.Description
				})
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
