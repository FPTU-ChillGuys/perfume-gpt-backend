using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockAdjustmentService
	{
		Task<BaseResponse<string>> CreateStockAdjustmentAsync(CreateStockAdjustmentRequest request, Guid userId);
		Task<BaseResponse<string>> VerifyStockAdjustmentAsync(Guid adjustmentId, VerifyStockAdjustmentRequest request, Guid verifiedByUserId);
		Task<BaseResponse<StockAdjustmentResponse>> GetStockAdjustmentByIdAsync(Guid id);
		Task<BaseResponse<PagedResult<StockAdjustmentListItem>>> GetPagedStockAdjustmentsAsync(GetPagedStockAdjustmentsRequest request);
		Task<BaseResponse<string>> UpdateAdjustmentStatusAsync(Guid id, UpdateStockAdjustmentStatusRequest request);
		Task<BaseResponse<bool>> DeleteStockAdjustmentAsync(Guid id);
	}
}
