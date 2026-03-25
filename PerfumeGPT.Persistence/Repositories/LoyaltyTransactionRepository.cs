using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;

namespace PerfumeGPT.Persistence.Repositories
{
	public class LoyaltyTransactionRepository : GenericRepository<LoyaltyTransaction>, ILoyaltyTransactionRepository
	{
		public LoyaltyTransactionRepository(PerfumeDbContext context) : base(context) { }

		public async Task<int> GetPointBalanceAsync(Guid userId)
			=> await _context.LoyaltyTransactions.Where(lt => lt.UserId == userId).SumAsync(lt => lt.PointsChanged);

		public async Task<(List<LoyaltyTransactionHistoryItemResponse> Items, int TotalCount)> GetPagedHistoryByUserAsync(Guid userId, GetPagedUserLoyaltyTransactionsRequest request)
		{
			var query = _context.LoyaltyTransactions
				.AsNoTracking()
				.Where(x => x.UserId == userId)
				.AsQueryable();

			if (request.TransactionType.HasValue)
				query = query.Where(x => x.TransactionType == request.TransactionType.Value);

			var totalCount = await query.CountAsync();

			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(x => x.Id)
				: query.OrderByDescending(x => x.Id);

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new LoyaltyTransactionHistoryItemResponse
				{
					Id = x.Id,
					UserId = x.UserId,
					VoucherId = x.VoucherId,
					OrderId = x.OrderId,
					TransactionType = x.TransactionType,
					PointsChanged = x.PointsChanged,
					AbsolutePoints = x.PointsChanged < 0 ? -x.PointsChanged : x.PointsChanged,
					Reason = x.Reason
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<LoyaltyTransactionHistoryItemResponse> Items, int TotalCount)> GetPagedHistoryAsync(GetPagedLoyaltyTransactionsRequest request)
		{
			var query = _context.LoyaltyTransactions.AsNoTracking().AsQueryable();

			if (request.UserId.HasValue)
				query = query.Where(x => x.UserId == request.UserId.Value);

			if (request.TransactionType.HasValue)
				query = query.Where(x => x.TransactionType == request.TransactionType.Value);

			var totalCount = await query.CountAsync();

			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(x => x.Id)
				: query.OrderByDescending(x => x.Id);

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new LoyaltyTransactionHistoryItemResponse
				{
					Id = x.Id,
					UserId = x.UserId,
					VoucherId = x.VoucherId,
					OrderId = x.OrderId,
					TransactionType = x.TransactionType,
					PointsChanged = x.PointsChanged,
					AbsolutePoints = x.PointsChanged < 0 ? -x.PointsChanged : x.PointsChanged,
					Reason = x.Reason
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<LoyaltyTransactionTotalsResponse> GetTotalsAsync(Guid userId)
		{
			var query = _context.LoyaltyTransactions.AsNoTracking().Where(x => x.UserId == userId);

			var totalEarned = await query.Where(x => x.PointsChanged > 0).SumAsync(x => (int?)x.PointsChanged) ?? 0;
			var totalSpent = await query.Where(x => x.PointsChanged < 0).SumAsync(x => (int?)-x.PointsChanged) ?? 0;
			var balance = await query.SumAsync(x => (int?)x.PointsChanged) ?? 0;
			var totalTransactions = await query.CountAsync();

			return new LoyaltyTransactionTotalsResponse
			{
				UserId = userId,
				TotalEarnedPoints = totalEarned,
				TotalSpentPoints = totalSpent,
				PointBalance = balance,
				TotalTransactions = totalTransactions
			};
		}
	}
}
