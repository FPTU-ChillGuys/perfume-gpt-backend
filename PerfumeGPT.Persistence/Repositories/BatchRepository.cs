using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class BatchRepository : GenericRepository<Batch>, IBatchRepository
	{
		public BatchRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<bool> DeductBathAsync(Guid variantId, int quantity)
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

				if (batch.RemainingQuantity >= remainingToDeduct)
				{
					batch.RemainingQuantity -= remainingToDeduct;
					remainingToDeduct = 0;
				}
				else
				{
					remainingToDeduct -= batch.RemainingQuantity;
					batch.RemainingQuantity = 0;
				}
			}

			if (remainingToDeduct > 0)
			{
				return false;
			}

			await _context.SaveChangesAsync();
			return true;
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
			.Where(b => b.VariantId == variantId)
			.OrderBy(b => b.ExpiryDate)
			.ToListAsync();
	}
}
}
