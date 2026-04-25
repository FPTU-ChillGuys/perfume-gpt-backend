using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Sales operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsSalesRepository : GenericRepository<OrderDetail>, INatsSalesRepository
{
	public NatsSalesRepository(PerfumeDbContext context) : base(context) { }

	public async Task<NatsSalesAnalyticsResponse?> GetSalesAnalyticsByVariantIdForNatsAsync(Guid variantId)
	{
		var now = DateTime.UtcNow;
		var sevenDaysAgo = now.AddDays(-7);
		var thirtyDaysAgo = now.AddDays(-30);
		var twoMonthsAgo = now.AddMonths(-2);

		// Get variant info
		var variant = await _context.ProductVariants
			.Where(v => v.Id == variantId)
			.Select(v => new
			{
				v.Sku,
				ProductName = v.Product.Name,
				v.VolumeMl,
				ConcentrationName = v.Concentration.Name,
				Type = v.Type.ToString(),
				BasePrice = v.BasePrice
			})
			.FirstOrDefaultAsync();

		if (variant == null)
		{
			return null;
		}

		// Get order details for this variant from completed orders in last 2 months
		var orderDetails = await _context.OrderDetails
			.Where(od => od.VariantId == variantId &&
						od.Order.Status == OrderStatus.Delivered &&
						od.Order.CreatedAt >= twoMonthsAgo)
			.ToListAsync();

		if (!orderDetails.Any())
		{
			return new NatsSalesAnalyticsResponse
			{
				VariantId = variantId.ToString(),
				TotalQuantitySold = 0,
				TotalRevenue = 0,
				AverageDailySales = 0,
				Last7DaysSales = 0,
				Last30DaysSales = 0,
				Trend = "NoData",
				Volatility = "Unknown",
				Sku = variant.Sku,
				ProductName = variant.ProductName,
				VolumeMl = variant.VolumeMl,
				Type = variant.Type,
				BasePrice = variant.BasePrice,
				Status = "Unknown",
				ConcentrationName = variant.ConcentrationName,
				DailySalesData = []
			};
		}

		var totalQuantitySold = orderDetails.Sum(od => od.Quantity);
		var totalRevenue = orderDetails.Sum(od => od.UnitPrice * od.Quantity);
		var last7DaysSales = orderDetails.Where(od => od.Order.CreatedAt >= sevenDaysAgo).Sum(od => od.Quantity);
		var last30DaysSales = orderDetails.Where(od => od.Order.CreatedAt >= thirtyDaysAgo).Sum(od => od.Quantity);
		var averageDailySales = Math.Round((double)totalQuantitySold / 60, 2);

		// Calculate daily sales data for last 30 days
		var dailySalesData = orderDetails
			.Where(od => od.Order.CreatedAt >= thirtyDaysAgo)
			.GroupBy(od => od.Order.CreatedAt.Date)
			.Select(g => new NatsDailySalesRecord
			{
				Date = g.Key.ToString("yyyy-MM-dd"),
				QuantitySold = g.Sum(od => od.Quantity),
				Revenue = g.Sum(od => od.UnitPrice * od.Quantity)
			})
			.OrderBy(d => d.Date)
			.ToList();

		// Calculate trend and volatility
		var trend = totalQuantitySold > 0 ? "Stable" : "NoData";
		var volatility = "Unknown";

		if (dailySalesData.Count >= 7)
		{
			var recentAvg = dailySalesData.TakeLast(7).Average(d => d.QuantitySold);
			var olderAvg = dailySalesData.Take(7).Average(d => d.QuantitySold);

			if (olderAvg > 0)
			{
				var changePercent = ((recentAvg - olderAvg) / olderAvg) * 100;
				trend = changePercent > 10 ? "Upward" : (changePercent < -10 ? "Downward" : "Stable");
			}

			var stdDev = CalculateStandardDeviation(dailySalesData.Select(d => (double)d.QuantitySold).ToList());
			volatility = stdDev < 2 ? "Low" : (stdDev < 5 ? "Medium" : "High");
		}

		return new NatsSalesAnalyticsResponse
		{
			VariantId = variantId.ToString(),
			TotalQuantitySold = totalQuantitySold,
			TotalRevenue = totalRevenue,
			AverageDailySales = averageDailySales,
			Last7DaysSales = last7DaysSales,
			Last30DaysSales = last30DaysSales,
			Trend = trend,
			Volatility = volatility,
			Sku = variant.Sku,
			ProductName = variant.ProductName,
			VolumeMl = variant.VolumeMl,
			Type = variant.Type,
			BasePrice = variant.BasePrice,
			Status = "Active",
			ConcentrationName = variant.ConcentrationName,
			DailySalesData = dailySalesData
		};
	}

	private static double CalculateStandardDeviation(List<double> values)
	{
		if (values.Count < 2) return 0;
		var avg = values.Average();
		var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
		return Math.Sqrt(sumOfSquares / values.Count);
	}
}
