using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AdminDashboardRepository : IAdminDashboardRepository
	{
		private readonly PerfumeDbContext _context;

		public AdminDashboardRepository(PerfumeDbContext context) { _context = context; }

		public async Task<RevenueSummaryResponse> GetRevenueSummaryAsync(DateTime fromDateUtc, DateTime toDateUtc)
		{
			var successfulTransactionsQuery = _context.PaymentTransactions
				.AsNoTracking()
				.Where(pt => pt.TransactionStatus == TransactionStatus.Success
					&& pt.TransactionType == TransactionType.Payment
					&& pt.CreatedAt >= fromDateUtc
					&& pt.CreatedAt <= toDateUtc);

			var refundedTransactionsQuery = _context.PaymentTransactions
				.AsNoTracking()
				.Where(pt => pt.TransactionType == TransactionType.Refund
					&& pt.TransactionStatus == TransactionStatus.Success
					&& pt.CreatedAt >= fromDateUtc
					&& pt.CreatedAt <= toDateUtc);

			var grossRevenue = await successfulTransactionsQuery.SumAsync(pt => (decimal?)pt.Amount) ?? 0;
			var refundedAmount = await refundedTransactionsQuery.SumAsync(pt => (decimal?)pt.Amount) ?? 0;
			var successfulTransactionsCount = await successfulTransactionsQuery.CountAsync();
			var paidOrdersCount = await successfulTransactionsQuery
				.Select(pt => pt.OrderId)
				.Distinct()
				.CountAsync();

			return new RevenueSummaryResponse
			{
				FromDate = fromDateUtc,
				ToDate = toDateUtc,
				GrossRevenue = grossRevenue,
				RefundedAmount = refundedAmount,
				NetRevenue = grossRevenue - refundedAmount,
				SuccessfulTransactionsCount = successfulTransactionsCount,
				PaidOrdersCount = paidOrdersCount
			};
		}

		public async Task<InventoryLevelsResponse> GetInventoryLevelsAsync(DateTime nowUtc, int expiringWithinDays)
		{
			var expiringDate = nowUtc.AddDays(expiringWithinDays);

			var stocksQuery = _context.Stocks
				.AsNoTracking();

			var totalVariants = await stocksQuery.CountAsync();
			var totalStockQuantity = await stocksQuery.SumAsync(s => (int?)s.TotalQuantity) ?? 0;
			var totalAvailableQuantity = await stocksQuery.SumAsync(s => (int?)(s.TotalQuantity - s.ReservedQuantity)) ?? 0;
			var lowStockVariantsCount = await stocksQuery.CountAsync(s => s.TotalQuantity <= s.LowStockThreshold);
			var outOfStockVariantsCount = await stocksQuery.CountAsync(s => s.TotalQuantity - s.ReservedQuantity <= 0);

			var batchesQuery = _context.Batches.AsNoTracking();
			var totalBatches = await batchesQuery.CountAsync();
			var expiredBatchesCount = await batchesQuery.CountAsync(b => b.ExpiryDate < nowUtc);
			var expiringSoonCount = await batchesQuery.CountAsync(b => b.ExpiryDate >= nowUtc && b.ExpiryDate <= expiringDate);

			return new InventoryLevelsResponse
			{
				TotalVariants = totalVariants,
				TotalStockQuantity = totalStockQuantity,
				TotalAvailableQuantity = totalAvailableQuantity,
				LowStockVariantsCount = lowStockVariantsCount,
				OutOfStockVariantsCount = outOfStockVariantsCount,
				TotalBatches = totalBatches,
				ExpiredBatchesCount = expiredBatchesCount,
				ExpiringSoonCount = expiringSoonCount
			};
		}

		public async Task<List<TopProductResponse>> GetTopProductsAsync(DateTime fromDateUtc, DateTime toDateUtc, int top)
		{
			var paidOrderIds = _context.PaymentTransactions
				.AsNoTracking()
				.AsSplitQuery()
				.Where(pt => pt.TransactionStatus == TransactionStatus.Success
					&& pt.CreatedAt >= fromDateUtc
					&& pt.CreatedAt <= toDateUtc)
				.Select(pt => pt.OrderId)
				.Distinct();

			var topProducts = await _context.OrderDetails
				.AsNoTracking()
				.Where(od => paidOrderIds.Contains(od.OrderId)
					&& !od.ProductVariant.IsDeleted
					&& !od.ProductVariant.Product.IsDeleted)
				.GroupBy(od => new
				{
					od.ProductVariant.ProductId,
					od.ProductVariant.Product.Name
				})
				.Select(g => new TopProductResponse
				{
					ProductId = g.Key.ProductId,
					ProductName = g.Key.Name,
					TotalUnitsSold = g.Sum(od => od.Quantity),
					Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
				})
				.OrderByDescending(tp => tp.TotalUnitsSold)
				.ThenByDescending(tp => tp.Revenue)
				.Take(top)
				.ToListAsync();

			return topProducts;
		}
	}
}
