using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IBatchRepository : IGenericRepository<Batch>
	{
		Task<List<Batch>> GetAvailableBatchesByVariantIdAsync(Guid variantId);
		Task<(List<BatchDetailResponse> Batches, int TotalCount)> GetBatchesAsync(GetBatchesRequest request);
		Task<BatchDetailResponse?> GetBatchByIdAsync(Guid batchId);
		Task<Batch?> GetBatchByIdWithIncludesAsync(Guid batchId);
		Task<Guid?> GetVariantIdByBatchIdAsync(Guid batchId);
		Task<List<BatchLookupResponse>> GetBatchLookupAsync();

		Task<bool> IncreaseBatchQuantityAsync(Guid batchId, int quantity);
		Task<bool> DecreaseBatchQuantityAsync(Guid batchId, int quantity);
		Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity);
	}
}

