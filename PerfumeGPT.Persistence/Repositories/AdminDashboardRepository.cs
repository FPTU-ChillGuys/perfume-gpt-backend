using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Commons;
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
			var dailyPayments = await successfulTransactionsQuery
				  .GroupBy(pt => pt.CreatedAt.Date)
				  .Select(g => new
				  {
					  Date = g.Key,
					  Amount = g.Sum(pt => pt.Amount)
				  })
				  .ToListAsync();
			var dailyRefunds = await refundedTransactionsQuery
				.GroupBy(pt => pt.CreatedAt.Date)
				.Select(g => new
				{
					Date = g.Key,
					Amount = g.Sum(pt => pt.Amount)
				})
				.ToListAsync();
			var paymentByDate = dailyPayments.ToDictionary(x => x.Date, x => x.Amount);
			var refundByDate = dailyRefunds.ToDictionary(x => x.Date, x => x.Amount);
			var fromDate = fromDateUtc.Date;
			var toDate = toDateUtc.Date;
			var chartData = Enumerable.Range(0, (toDate - fromDate).Days + 1)
				.Select(offset =>
				{
					var date = fromDate.AddDays(offset);
					paymentByDate.TryGetValue(date, out var paymentAmount);
					refundByDate.TryGetValue(date, out var refundAmount);
					return new DailyRevenueItem
					{
						Date = date,
						GrossRevenue = paymentAmount,
						RefundedAmount = refundAmount,
						NetRevenue = paymentAmount + refundAmount
					};
				})
				.ToList();
			var paymentMethodStats = await successfulTransactionsQuery
				.GroupBy(pt => pt.Method)
				.Select(g => new
				{
					PaymentMethod = g.Key,
					TransactionsCount = g.Count(),
					Amount = g.Sum(pt => pt.Amount)
				})
				.ToListAsync();
			var paymentMethodDistribution = Enum.GetValues<PaymentMethod>()
				.Select(method =>
				{
					var stat = paymentMethodStats.FirstOrDefault(x => x.PaymentMethod == method);
					return new PaymentMethodDistributionResponse
					{
						PaymentMethod = method,
						TransactionsCount = stat?.TransactionsCount ?? 0,
						Amount = stat?.Amount ?? 0
					};
				})
				.ToList();

			return new RevenueSummaryResponse
			{
				FromDate = fromDateUtc,
				ToDate = toDateUtc,
				GrossRevenue = grossRevenue,
				RefundedAmount = refundedAmount,
				NetRevenue = grossRevenue + refundedAmount,
				SuccessfulTransactionsCount = successfulTransactionsCount,
				PaidOrdersCount = paidOrdersCount,
				PaymentMethodDistribution = paymentMethodDistribution,
				ChartData = chartData
			};
		}

		public async Task<InventoryLevelsResponse> GetInventoryLevelsAsync(DateTime nowUtc, int expiringWithinDays, SellableStockQueryContext sellable)
		{
			var expiringDate = nowUtc.AddDays(expiringWithinDays);
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

			var stocksQuery = _context.Stocks
				.AsNoTracking()
				.Where(s => !s.ProductVariant.IsDeleted);

			var totalVariants = await stocksQuery.CountAsync();
			var totalStockQuantity = await stocksQuery.SumAsync(s => (int?)s.TotalQuantity) ?? 0;
			var totalAvailableQuantity = await stocksQuery.SumAsync(s =>
				s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0));

			var lowStockVariantsCount = await stocksQuery.CountAsync(s =>
				s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0) > 0
				&& s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0) <= s.LowStockThreshold);

			var outOfStockVariantsCount = await stocksQuery.CountAsync(s =>
				s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0) <= 0);

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
				.Where(od => od.Order.PaymentStatus == PaymentStatus.Paid
					&& od.Order.CreatedAt >= fromDateUtc
					&& od.Order.CreatedAt <= toDateUtc
					&& !od.ProductVariant.IsDeleted
					&& !od.ProductVariant.Product.IsDeleted)
				.GroupBy(od => new
				{
					od.ProductVariant.ProductId,
					od.ProductVariant.Product.Name,
					PrimaryImage = od.ProductVariant.Product.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url)
						.FirstOrDefault()
				})
				.Select(g => new TopProductResponse
				{
					ProductId = g.Key.ProductId,
					ProductName = g.Key.Name,
					ImageUrl = g.Key.PrimaryImage,
					TotalUnitsSold = g.Sum(od => od.Quantity),
					Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
				})
				.OrderByDescending(tp => tp.TotalUnitsSold)
				.Take(top)
				.ToListAsync();

			return topProducts;
		}
	}
}
