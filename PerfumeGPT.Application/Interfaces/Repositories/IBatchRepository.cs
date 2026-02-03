using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IBatchRepository : IGenericRepository<Batch>
	{
		Task<bool> DeductBatchAsync(Guid variantId, int quantity);
		Task<bool> IsValidForDeductionAsync(Guid variantId, int requiredQuantity);
		Task<List<Batch>> GetAvailableBatchesByVariantAsync(Guid variantId);
		Task<(List<Batch> Batches, int TotalCount)> GetBatchesWithFiltersAsync(GetBatchesRequest request);
		Task<List<Batch>> GetBatchesByVariantWithIncludesAsync(Guid variantId);
		Task<Batch?> GetBatchByIdWithIncludesAsync(Guid batchId);
	}
}

