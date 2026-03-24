using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockService
	{
		Task InitStockAsync(Guid variantId, int initialQuantity, int lowThreshold);
		Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity);
		Task IncreaseStockAsync(Guid variantId, int quantity);
		Task DecreaseStockAsync(Guid variantId, int quantity);
		Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetPagedInventoryRequest request);
		Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId);
		Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync();
	}
}

