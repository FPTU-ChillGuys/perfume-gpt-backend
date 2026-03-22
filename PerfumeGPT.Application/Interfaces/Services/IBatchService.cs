using PerfumeGPT.Application.DTOs.Requests.Batches;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBatchService
	{
		Task<List<Batch>> CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests);

		// Validation methods
		bool IsTotalQuantityValid(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity);
		Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity);

		// Retrieval methods
		Task<BaseResponse<List<BatchLookupResponse>>> GetBatchLookupAsync();
		Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request);
		Task<BaseResponse<List<BatchDetailResponse>>> GetBatchesByVariantIdAsync(Guid variantId);
		Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId);

		// Calculation methods
		Task<bool> IncreaseBatchQuantityAsync(Guid batchId, int quantity);
		Task<bool> DecreaseBatchQuantityAsync(Guid batchId, int quantity);
		Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity);
	}
}
