using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockService
	{
		/// <summary>
		/// Validates if the requested quantity is available in stock for the variant.
		/// </summary>
		/// <param name="variantId">The variant ID to check stock for</param>
		/// <param name="requiredQuantity">The quantity required</param>
		/// <returns>True if stock is available, false otherwise</returns>
		Task<bool> IsValidToCartAsync(Guid variantId, int requiredQuantity);

		/// <summary>
		/// Increases stock quantity for a variant when items are imported.
		/// Creates new stock record if it doesn't exist.
		/// NOTE: Does NOT call SaveChanges - caller must save changes (transaction orchestrator).
		/// </summary>
		/// <param name="variantId">The variant ID to update stock for</param>
		/// <param name="quantity">The quantity to add</param>
		/// <returns>True if stock was tracked successfully, false if validation failed</returns>
		Task<bool> IncreaseStockAsync(Guid variantId, int quantity);

		/// <summary>
		/// Decreases stock quantity for a variant when items are sold or removed.
		/// Ensures stock doesn't go below zero.
		/// NOTE: Does NOT call SaveChanges - caller must save changes (transaction orchestrator).
		/// </summary>
		/// <param name="variantId">The variant ID to update stock for</param>
		/// <param name="quantity">The quantity to subtract</param>
		/// <returns>True if stock was tracked successfully, false if validation failed or stock doesn't exist</returns>
		Task<bool> DecreaseStockAsync(Guid variantId, int quantity);

		/// <summary>
		/// Gets paginated list of stock inventory.
		/// </summary>
		/// <param name="request">Inventory filter and pagination request</param>
		/// <returns>Paged result of stock items</returns>
		Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetInventoryRequest request);

		/// <summary>
		/// Gets stock details for a specific variant.
		/// </summary>
		/// <param name="variantId">The variant ID</param>
		/// <returns>Stock details</returns>
		Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId);

		/// <summary>
		/// Gets inventory summary statistics.
		/// </summary>
		/// <returns>Inventory summary</returns>
		Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync();
	}
}

