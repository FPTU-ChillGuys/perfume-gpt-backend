using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBatchService
	{
		Task CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests);

		// Retrieval methods
		Task<BaseResponse<List<BatchLookupResponse>>> GetBatchLookupAsync();
		Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request);
		Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId);
	}
}
