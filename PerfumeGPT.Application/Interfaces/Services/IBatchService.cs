using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBatchService
	{
		Task CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests);

		// Validation methods
		Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity);

		// Retrieval methods
		Task<BaseResponse<List<BatchLookupResponse>>> GetBatchLookupAsync();
		Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request);
		Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId);

		// Calculation methods
		Task IncreaseBatchQuantityAsync(Guid batchId, int quantity);
		Task DecreaseBatchQuantityAsync(Guid batchId, int quantity);
	}
}
