using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

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
			Expression<Func<Batch, bool>> filter = x => true;

			if (request.VariantId.HasValue)
			{
				var variantId = request.VariantId.Value;
				filter = filter.AndAlso(b => b.VariantId == variantId);
			}

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();
				var likePattern = $"%{searchTerm}%";
				Expression<Func<Batch, bool>> searchFilter = b => false;
				searchFilter = searchFilter.OrElse(b => EF.Functions.Like(b.BatchCode, likePattern));
				searchFilter = searchFilter.OrElse(b => EF.Functions.Like(b.ProductVariant.Sku, likePattern));
				searchFilter = searchFilter.OrElse(b => EF.Functions.Like(b.ProductVariant.Product.Name, likePattern));
				filter = filter.AndAlso(searchFilter);
			}

			if (request.IsExpired.HasValue)
			{
				filter = request.IsExpired.Value
					? filter.AndAlso(b => b.ExpiryDate < now)
					: filter.AndAlso(b => b.ExpiryDate >= now);
			}

			if (request.IsExpiringSoon.HasValue)
			{
				filter = request.IsExpiringSoon.Value
					? filter.AndAlso(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate)
					: filter.AndAlso(b => b.ExpiryDate < now || b.ExpiryDate > expiringSoonDate);
			}

			query = query.Where(filter);

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(Batch.BatchCode),
				nameof(Batch.ManufactureDate),
				nameof(Batch.ExpiryDate),
				nameof(Batch.ImportQuantity),
				nameof(Batch.RemainingQuantity),
				nameof(Batch.CreatedAt)
			};
			var sortBy = request.SortBy?.Trim();
			sortBy = !string.IsNullOrWhiteSpace(sortBy)
				? (sortBy.Length == 1
					? char.ToUpper(sortBy[0]).ToString()
					: char.ToUpper(sortBy[0]) + sortBy.Substring(1))
				: null;
			var isDescending = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
			query = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, isDescending)
				: query.OrderBy(b => b.ExpiryDate).ThenByDescending(b => b.CreatedAt);

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

		public async Task<List<Batch>> GetBatchesByIds(List<Guid> ids)
		{
			return await _context.Batches
				.Where(b => ids.Contains(b.Id))
				.Include(b => b.ImportDetail)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<Batch>> GetByVariantAndBatchCodesAsync(IEnumerable<(Guid VariantId, string BatchCode)> keys)
		{
			var normalizedKeys = keys
				.Where(x => x.VariantId != Guid.Empty && !string.IsNullOrWhiteSpace(x.BatchCode))
				.Select(x => new { x.VariantId, BatchCode = x.BatchCode.Trim() })
				.Distinct()
				.ToList();

			if (normalizedKeys.Count == 0)
			{
				return [];
			}

			var variantIds = normalizedKeys.Select(x => x.VariantId).Distinct().ToList();
			var batchCodes = normalizedKeys.Select(x => x.BatchCode).Distinct().ToList();

			return await _context.Batches
				.Where(b => variantIds.Contains(b.VariantId) && batchCodes.Contains(b.BatchCode))
				.AsNoTracking()
				.ToListAsync();
		}
	}
}
