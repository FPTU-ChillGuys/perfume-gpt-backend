using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
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
		private const int MaxRetryAttempts = 3;
		private const int RetryDelayMilliseconds = 100;

		public BatchRepository(PerfumeDbContext context) : base(context)
		{
		}

		#region Query Methods

		public async Task<List<Batch>> GetAvailableBatchesByVariantIdAsync(Guid variantId)
		{
			return await _context.Batches
				.Where(b => b.VariantId == variantId
					&& b.ExpiryDate > DateTime.UtcNow
					&& (b.RemainingQuantity - b.ReservedQuantity) > 0)
				.OrderBy(b => b.ExpiryDate)
				.ToListAsync();
		}

		public async Task<(List<BatchDetailResponse> Batches, int TotalCount)> GetBatchesAsync(GetBatchesRequest request)
		{
			var query = BuildBatchesQuery(request);

			var totalCount = await query.CountAsync();

			var batches = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<BatchDetailResponse>()
				.ToListAsync();

			return (batches, totalCount);
		}

		public async Task<List<BatchDetailResponse>> GetBatchesByVariantIdAsync(Guid variantId)
		{
			return await _context.Batches
				.Where(b => b.VariantId == variantId)
				.OrderBy(b => b.ExpiryDate)
				.AsNoTracking()
				.ProjectToType<BatchDetailResponse>()
				.ToListAsync();
		}

		public async Task<BatchDetailResponse?> GetBatchByIdAsync(Guid batchId)
		{
			return await _context.Batches
				.Where(b => b.Id == batchId)
				.AsNoTracking()
				.ProjectToType<BatchDetailResponse>()
				.FirstOrDefaultAsync();
		}

		public async Task<Batch?> GetBatchByIdWithIncludesAsync(Guid batchId)
		{
			return await _context.Batches
				.Include(b => b.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(b => b.ProductVariant.Concentration)
				.Include(b => b.ImportDetail)
				.FirstOrDefaultAsync(b => b.Id == batchId);
		}

		#endregion

		#region Quantity Operations with Optimistic Concurrency

		public async Task<bool> IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0) return false;

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					var batch = await _context.Batches.FindAsync(batchId);
					if (batch == null) return false;

					var newQuantity = batch.RemainingQuantity + quantity;
					if (newQuantity > batch.ImportQuantity)
					{
						return false;
					}

					batch.RemainingQuantity = newQuantity;
					await _context.SaveChangesAsync();
					return true;
				}
				catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
				{
					await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
					DetachAllEntities();
				}
			}
			return false;
		}

		public async Task<bool> DecreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0) return false;

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					var batch = await _context.Batches.FindAsync(batchId);
					if (batch == null) return false;

					if (batch.RemainingQuantity < quantity)
					{
						return false;
					}

					batch.RemainingQuantity -= quantity;
					await _context.SaveChangesAsync();
					return true;
				}
				catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
				{
					await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
					DetachAllEntities();
				}
			}
			return false;
		}

		public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity)
		{
			if (quantity <= 0) return false;

			for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
			{
				try
				{
					var availableBatches = await _context.Batches
						.Where(b => b.VariantId == variantId
							&& b.ExpiryDate > DateTime.UtcNow
							&& (b.RemainingQuantity - b.ReservedQuantity) > 0)
						.OrderBy(b => b.ExpiryDate)
						.ToListAsync();

					var remainingToDeduct = quantity;

					foreach (var batch in availableBatches)
					{
						if (remainingToDeduct <= 0) break;

						var deductAmount = Math.Min(batch.AvailableInBatch, remainingToDeduct);

						batch.RemainingQuantity -= deductAmount;
						remainingToDeduct -= deductAmount;
					}

					if (remainingToDeduct > 0)
					{
						DetachAllEntities();
						return false;
					}

					await _context.SaveChangesAsync();
					return true;
				}
				catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
				{
					await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
					DetachAllEntities();
				}
			}
			return false;
		}

		#endregion

		#region Private Helpers

		private void DetachAllEntities()
		{
			foreach (var entry in _context.ChangeTracker.Entries())
			{
				entry.State = EntityState.Detached;
			}
		}

		private IQueryable<Batch> BuildBatchesQuery(GetBatchesRequest request)
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			Expression<Func<Batch, bool>> filter = b => true;

			if (request.VariantId.HasValue)
			{
				filter = filter.AndAlso(b => b.VariantId == request.VariantId.Value);
			}

			if (!string.IsNullOrEmpty(request.SearchTerm))
			{
				Expression<Func<Batch, bool>> searchFilter = b =>
					(b.BatchCode != null && b.BatchCode.Contains(request.SearchTerm)) ||
					(b.ProductVariant.Sku != null && b.ProductVariant.Sku.Contains(request.SearchTerm)) ||
					(b.ProductVariant.Product.Name != null && b.ProductVariant.Product.Name.Contains(request.SearchTerm));

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

			var query = _context.Batches
				.Where(filter)
				.AsNoTracking();

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

			return query;
		}

		#endregion
	}
}
