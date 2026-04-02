using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class BatchRepository : GenericRepository<Batch>, IBatchRepository
	{
		public BatchRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<Batch>> GetAvailableBatchesByVariantIdAsync(Guid variantId)
		=> await _context.Batches
			.Where(b => b.VariantId == variantId
				&& b.ExpiryDate > DateTime.UtcNow
				&& (b.RemainingQuantity - b.ReservedQuantity) > 0)
			.OrderBy(b => b.ExpiryDate)
			.ToListAsync();

		public async Task<(List<BatchDetailResponse> Batches, int TotalCount)> GetBatchesAsync(GetBatchesRequest request)
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			var query = _context.Batches.AsNoTracking();

			if (request.VariantId.HasValue)
			{
				query = query.Where(b => b.VariantId == request.VariantId.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();
				var likePattern = $"%{searchTerm}%";

				query = query.Where(b =>
					EF.Functions.Like(b.BatchCode, likePattern)
					|| EF.Functions.Like(b.ProductVariant.Sku, likePattern)
					|| EF.Functions.Like(b.ProductVariant.Product.Name, likePattern));
			}

			if (request.IsExpired.HasValue)
			{
				query = request.IsExpired.Value
					? query.Where(b => b.ExpiryDate < now)
					: query.Where(b => b.ExpiryDate >= now);
			}

			if (request.IsExpiringSoon.HasValue)
			{
				query = request.IsExpiringSoon.Value
					? query.Where(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate)
					: query.Where(b => b.ExpiryDate < now || b.ExpiryDate > expiringSoonDate);
			}

			if (!string.IsNullOrEmpty(request.SortBy))
			{
				var descending = request.SortOrder?.ToLower() == "desc";
				query = query.ApplySorting(request.SortBy, descending);
			}
			else
			{
				query = query.OrderBy(b => b.ExpiryDate)
					.ThenByDescending(b => b.CreatedAt);
			}

			var totalCount = await query.CountAsync();

			var batches = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
			   .Select(b => new BatchDetailResponse
			   {
				   Id = b.Id,
				   BatchCode = b.BatchCode,
				   ManufactureDate = b.ManufactureDate,
				   ExpiryDate = b.ExpiryDate,
				   ImportQuantity = b.ImportQuantity,
				   RemainingQuantity = b.RemainingQuantity,
				   CreatedAt = b.CreatedAt,
				   VariantId = b.VariantId,
				   VariantSku = b.ProductVariant.Sku,
				   ProductName = b.ProductVariant.Product.Name,
				   VolumeMl = b.ProductVariant.VolumeMl,
				   ConcentrationName = b.ProductVariant.Concentration.Name
			   })
				.ToListAsync();

			return (batches, totalCount);
		}

		public async Task<List<BatchLookupResponse>> GetBatchLookupAsync()
		=> await _context.Batches
		   .Select(b => new BatchLookupResponse
		   {
			   Id = b.Id,
			   BatchCode = b.BatchCode,
			   VariantId = b.VariantId,
			   Sku = b.ProductVariant.Sku
		   })
			.AsNoTracking().ToListAsync();

		public async Task<BatchDetailResponse?> GetBatchByIdAsync(Guid batchId)
		=> await _context.Batches
			.Where(b => b.Id == batchId)
			.AsNoTracking()
		   .Select(b => new BatchDetailResponse
		   {
			   Id = b.Id,
			   BatchCode = b.BatchCode,
			   ManufactureDate = b.ManufactureDate,
			   ExpiryDate = b.ExpiryDate,
			   ImportQuantity = b.ImportQuantity,
			   RemainingQuantity = b.RemainingQuantity,
			   CreatedAt = b.CreatedAt,
			   VariantId = b.VariantId,
			   VariantSku = b.ProductVariant.Sku,
			   ProductName = b.ProductVariant.Product.Name,
			   VolumeMl = b.ProductVariant.VolumeMl,
			   ConcentrationName = b.ProductVariant.Concentration.Name
		   })
			.FirstOrDefaultAsync();

		public async Task<Batch?> GetBatchByIdWithIncludesAsync(Guid batchId)
		=> await _context.Batches
			.AsSplitQuery()
			.Include(b => b.ProductVariant)
				.ThenInclude(v => v.Product)
			.Include(b => b.ProductVariant.Concentration)
			.Include(b => b.ImportDetail)
			.FirstOrDefaultAsync(b => b.Id == batchId);

		public async Task<Guid?> GetVariantIdByBatchIdAsync(Guid batchId)
		=> await _context.Batches
			.AsNoTracking()
			.Where(b => b.Id == batchId)
			.Select(b => (Guid?)b.VariantId)
			.FirstOrDefaultAsync();

		public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity)
		{
			var now = DateTime.UtcNow;
			var availableBatches = await _context.Batches
				.Where(b => b.VariantId == variantId
					&& b.ExpiryDate > now
					&& (b.RemainingQuantity - b.ReservedQuantity) > 0)
				.OrderBy(b => b.ExpiryDate)
				.ToListAsync();

			var totalAvailable = availableBatches.Sum(b => b.AvailableInBatch);
			if (totalAvailable < quantity)
			{
				return false;
			}

			var remainingToDeduct = quantity;
			foreach (var batch in availableBatches)
			{
				if (remainingToDeduct <= 0) break;

				var deductAmount = Math.Min(batch.AvailableInBatch, remainingToDeduct);
				batch.DecreaseQuantity(deductAmount);
				remainingToDeduct -= deductAmount;
			}
			return true;
		}
	}
}
