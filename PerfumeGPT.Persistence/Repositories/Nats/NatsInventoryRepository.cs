using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Inventory operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsInventoryRepository : GenericRepository<Stock>, INatsInventoryRepository
{
	public NatsInventoryRepository(PerfumeDbContext context) : base(context) { }

	public async Task<(List<NatsInventoryStockResponse> Items, int TotalCount)> GetPagedInventoryForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		int? brandId = null,
		int? categoryId = null,
		string? stockStatus = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var query = _context.Stocks
			.Where(s => s.ProductVariant != null && !s.ProductVariant.IsDeleted)
			.AsQueryable();

		if (variantId.HasValue)
		{
			query = query.Where(s => s.VariantId == variantId.Value);
		}

		if (brandId.HasValue)
		{
			query = query.Where(s => s.ProductVariant.Product.BrandId == brandId.Value);
		}

		if (categoryId.HasValue)
		{
			query = query.Where(s => s.ProductVariant.Product.CategoryId == categoryId.Value);
		}

		if (!string.IsNullOrWhiteSpace(stockStatus))
		{
			query = stockStatus.ToLower() switch
			{
				"lowstock" => query.Where(s => s.TotalQuantity <= s.LowStockThreshold),
				"outofstock" => query.Where(s => s.TotalQuantity == 0),
				"available" => query.Where(s => s.TotalQuantity > s.LowStockThreshold),
				_ => query
			};
		}

		var totalCount = await query.CountAsync();

		// Apply sorting
		query = string.IsNullOrWhiteSpace(sortBy) switch
		{
			false when sortBy.Equals("quantity", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(s => s.TotalQuantity) : query.OrderBy(s => s.TotalQuantity),
			false when sortBy.Equals("productname", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(s => s.ProductVariant.Product.Name) : query.OrderBy(s => s.ProductVariant.Product.Name),
			_ => query.OrderByDescending(s => s.ProductVariant.Product.Name)
		};

		var items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.Select(s => new NatsInventoryStockResponse
			{
				ConcentrationName = s.ProductVariant.Concentration.Name,
				BasePrice = s.ProductVariant.BasePrice,
				Id = s.Id.ToString(),
				IsLowStock = s.TotalQuantity <= s.LowStockThreshold,
				LowStockThreshold = s.LowStockThreshold,
				ProductName = s.ProductVariant.Product.Name,
				TotalQuantity = s.TotalQuantity,
				VariantId = s.VariantId.ToString(),
				VariantSku = s.ProductVariant.Sku,
				VolumeMl = s.ProductVariant.VolumeMl,
				AvailableQuantity = s.AvailableQuantity,
				VariantStatus = s.ProductVariant.Status.ToString(),
				Status = s.Status.ToString(),
				Type = s.ProductVariant.Type.ToString(),
				ReservedQuantity = s.ReservedQuantity
			})
			.AsNoTracking()
			.ToListAsync();

		return (items, totalCount);
	}

	public async Task<NatsInventoryOverallStats> GetOverallStatsForNatsAsync()
	{
		var stats = await _context.Stocks
			.Where(s => s.ProductVariant != null && !s.ProductVariant.IsDeleted)
			.GroupBy(s => 1)
			.Select(g => new
			{
				TotalSku = g.Count(),
				LowStockSku = g.Count(s => s.TotalQuantity <= s.LowStockThreshold && s.TotalQuantity > 0),
				OutOfStockSku = g.Count(s => s.TotalQuantity == 0)
			})
			.FirstOrDefaultAsync();

		if (stats == null)
		{
			return new NatsInventoryOverallStats
			{
				TotalSku = 0,
				LowStockSku = 0,
				OutOfStockSku = 0,
				ExpiredBatches = 0,
				NearExpiryBatches = 0,
				CriticalAlerts = 0
			};
		}

		return new NatsInventoryOverallStats
		{
			TotalSku = stats.TotalSku,
			LowStockSku = stats.LowStockSku,
			OutOfStockSku = stats.OutOfStockSku,
			ExpiredBatches = 0,
			NearExpiryBatches = 0,
			CriticalAlerts = stats.OutOfStockSku
		};
	}

	public async Task<NatsInventoryStockResponse?> GetVariantInventoryForNatsAsync(Guid variantId)
	{
		return await _context.Stocks
			.Where(s => s.VariantId == variantId && s.ProductVariant != null && !s.ProductVariant.IsDeleted)
			.Select(s => new NatsInventoryStockResponse
			{
				ConcentrationName = s.ProductVariant.Concentration.Name,
				BasePrice = s.ProductVariant.BasePrice,
				Id = s.Id.ToString(),
				IsLowStock = s.TotalQuantity <= s.LowStockThreshold,
				LowStockThreshold = s.LowStockThreshold,
				ProductName = s.ProductVariant.Product.Name,
				TotalQuantity = s.TotalQuantity,
				VariantId = s.VariantId.ToString(),
				VariantSku = s.ProductVariant.Sku,
				VolumeMl = s.ProductVariant.VolumeMl,
				AvailableQuantity = s.AvailableQuantity,
				VariantStatus = s.ProductVariant.Status.ToString(),
				Status = s.Status.ToString(),
				Type = s.ProductVariant.Type.ToString(),
				ReservedQuantity = s.ReservedQuantity
			})
			.AsNoTracking()
			.FirstOrDefaultAsync();
	}

	public async Task<List<NatsInventoryStockResponse>> GetLowStockVariantsForNatsAsync(int threshold)
	{
		return await _context.Stocks
			.Where(s => s.ProductVariant != null && !s.ProductVariant.IsDeleted && s.TotalQuantity <= s.LowStockThreshold)
			.Select(s => new NatsInventoryStockResponse
			{
				ConcentrationName = s.ProductVariant.Concentration.Name,
				BasePrice = s.ProductVariant.BasePrice,
				Id = s.Id.ToString(),
				IsLowStock = s.TotalQuantity <= s.LowStockThreshold,
				LowStockThreshold = s.LowStockThreshold,
				ProductName = s.ProductVariant.Product.Name,
				TotalQuantity = s.TotalQuantity,
				VariantId = s.VariantId.ToString(),
				VariantSku = s.ProductVariant.Sku,
				VolumeMl = s.ProductVariant.VolumeMl,
				AvailableQuantity = s.AvailableQuantity,
				VariantStatus = s.ProductVariant.Status.ToString(),
				Status = s.Status.ToString(),
				Type = s.ProductVariant.Type.ToString(),
				ReservedQuantity = s.ReservedQuantity
			})
			.OrderBy(s => s.TotalQuantity)
			.AsNoTracking()
			.ToListAsync();
	}
}
