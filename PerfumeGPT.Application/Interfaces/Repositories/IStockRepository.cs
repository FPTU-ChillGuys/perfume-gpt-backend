using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Commons;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockRepository : IGenericRepository<Stock>
	{
		Task UpdateStockAsync(Guid variantId);
		Task<bool> IsLowStockAsync(Guid variantId, SellableStockQueryContext sellable);
		Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity, int? minBufferDays = null, IEnumerable<Guid>? exemptedBatchIds = null);
		Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request, SellableStockQueryContext sellable);
		Task<StockResponse?> GetStockWithDetailsByVariantIdAsync(Guid variantId, SellableStockQueryContext sellable);
		Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount)> GetInventorySummaryDataAsync(SellableStockQueryContext sellable);
		Task<List<LowStockAlertItem>> GetLowStockAlertItemsAsync(SellableStockQueryContext sellable);
	}
}
