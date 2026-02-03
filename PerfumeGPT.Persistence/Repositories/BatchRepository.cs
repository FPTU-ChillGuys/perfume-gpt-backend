using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Data;
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

		public async Task<bool> DeductBatchAsync(Guid variantId, int quantity)
		{
			var strategy = _context.Database.CreateExecutionStrategy();

			return await strategy.ExecuteAsync(async () =>
			{
				for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
				{
					try
					{
						using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

						try
						{
							var variantBatches = await _context.Batches
								.Where(b => b.VariantId == variantId && b.ExpiryDate > DateTime.UtcNow && b.RemainingQuantity > 0)
								.OrderBy(b => b.ExpiryDate)
								.ToListAsync();

							var remainingToDeduct = quantity;

							foreach (var batch in variantBatches)
							{
								if (remainingToDeduct <= 0)
								{
									break;
								}

								var deductAmount = Math.Min(batch.RemainingQuantity, remainingToDeduct);

								var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
									$"UPDATE Batches SET RemainingQuantity = RemainingQuantity - {deductAmount} WHERE Id = {batch.Id} AND RemainingQuantity >= {deductAmount}");

								if (rowsAffected == 0)
								{
									await transaction.RollbackAsync();
									return false;
								}

								remainingToDeduct -= deductAmount;
							}

							if (remainingToDeduct > 0)
							{
								await transaction.RollbackAsync();
								return false;
							}

							await transaction.CommitAsync();
							return true;
						}
						catch
						{
							await transaction.RollbackAsync();
							throw;
						}
					}
					catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
					{
						await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
						continue;
					}
				}

				return false;
			});
		}

		public async Task<bool> IsValidForDeductionAsync(Guid variantId, int requiredQuantity)
		{
			var totalAvailable = await _context.Batches
				.Where(b => b.VariantId == variantId && b.ExpiryDate > DateTime.UtcNow)
				.SumAsync(b => b.RemainingQuantity);

			return totalAvailable >= requiredQuantity;
		}

		public async Task<List<Batch>> GetAvailableBatchesByVariantAsync(Guid variantId)
		{
			return await _context.Batches
				.Where(b => b.VariantId == variantId
					&& b.ExpiryDate > DateTime.UtcNow
					&& (b.RemainingQuantity - b.ReservedQuantity) > 0
				)
				.OrderBy(b => b.ExpiryDate)
				.ToListAsync();
		}

		public async Task<(List<Batch> Batches, int TotalCount)> GetBatchesWithFiltersAsync(GetBatchesRequest request)
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			// Build filter expression step by step using AndAlso extension
			Expression<Func<Batch, bool>> filter = b => true;

			// Filter by VariantId
			if (request.VariantId.HasValue)
			{
				filter = filter.AndAlso(b => b.VariantId == request.VariantId.Value);
			}

			// Filter by SearchTerm (BatchCode, SKU, or Product Name)
			if (!string.IsNullOrEmpty(request.SearchTerm))
			{
				Expression<Func<Batch, bool>> searchFilter = b =>
					(b.BatchCode != null && b.BatchCode.Contains(request.SearchTerm)) ||
					(b.ProductVariant.Sku != null && b.ProductVariant.Sku.Contains(request.SearchTerm)) ||
					(b.ProductVariant.Product.Name != null && b.ProductVariant.Product.Name.Contains(request.SearchTerm));

				filter = filter.AndAlso(searchFilter);
			}

			// Filter by IsExpired
			if (request.IsExpired.HasValue)
			{
				if (request.IsExpired.Value)
				{
					filter = filter.AndAlso(b => b.ExpiryDate < now);
				}
				else
				{
					filter = filter.AndAlso(b => b.ExpiryDate >= now);
				}
			}

			// Filter by IsExpiringSoon (within 30 days)
			if (request.IsExpiringSoon.HasValue)
			{
				if (request.IsExpiringSoon.Value)
				{
					// Not expired yet AND expires within 30 days
					filter = filter.AndAlso(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate);
				}
				else
				{
					// Either already expired OR expires after 30 days
					filter = filter.AndAlso(b => b.ExpiryDate < now || b.ExpiryDate > expiringSoonDate);
				}
			}

			// Build query with includes
			var query = _context.Batches
				.Include(b => b.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(b => b.ProductVariant.Concentration)
				.Where(filter)
				.AsNoTracking();

			// Get total count before pagination
			var totalCount = await query.CountAsync();

			// Apply sorting
			if (!string.IsNullOrEmpty(request.SortBy))
			{
				var descending = request.SortOrder?.ToLower() == "desc";
				query = query.ApplySorting(request.SortBy, descending);
			}
			else
			{
				// Default sorting: earliest expiry first (FIFO)
				query = query.OrderBy(b => b.ExpiryDate)
					.ThenBy(b => b.ProductVariant.Product.Name);
			}

			// Apply pagination
			var batches = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (batches, totalCount);
		}

		public async Task<List<Batch>> GetBatchesByVariantWithIncludesAsync(Guid variantId)
		{
			return await _context.Batches
				.Include(b => b.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(b => b.ProductVariant.Concentration)
				.Where(b => b.VariantId == variantId)
				.OrderBy(b => b.ExpiryDate)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Batch?> GetBatchByIdWithIncludesAsync(Guid batchId)
		{
			return await _context.Batches
				.Include(b => b.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(b => b.ProductVariant.Concentration)
				.AsNoTracking()
				.FirstOrDefaultAsync(b => b.Id == batchId);
		}
	}
}
