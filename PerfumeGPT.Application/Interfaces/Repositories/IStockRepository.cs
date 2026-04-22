using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockRepository : IGenericRepository<Stock>
	{
		Task UpdateStockAsync(Guid variantId);
		Task<bool> IsLowStockAsync(Guid variantId);
		Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity);

		Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request);
		Task<StockResponse?> GetStockWithDetailsByVariantIdAsync(Guid variantId);
		Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount, int OutOfStockVariantsCount)> GetInventorySummaryDataAsync();
		Task<List<LowStockAlertItem>> GetLowStockAlertItemsAsync();
	}
}
