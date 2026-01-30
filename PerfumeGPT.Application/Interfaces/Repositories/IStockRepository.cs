using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockRepository : IGenericRepository<Stock>
	{
		Task<bool> UpdateStockAsync(Guid variantId);
		Task<bool> IsLowStockAsync(Guid variantId);
		Task<bool> IsValidToCart(Guid variantId, int requiredQuantity);

		/// <summary>
		/// Gets paged inventory with filtering, sorting, and includes.
		/// </summary>
		Task<(IEnumerable<Stock> Stocks, int TotalCount)> GetPagedInventoryAsync(
			Guid? variantId,
			string? searchTerm,
			bool? isLowStock,
			string? sortBy,
			string? sortOrder,
			int pageNumber,
			int pageSize);

		/// <summary>
		/// Gets stock by variant ID with all related entities included.
		/// </summary>
		Task<Stock?> GetStockWithDetailsByVariantIdAsync(Guid variantId);

		/// <summary>
		/// Gets inventory summary data (total variants, quantities, low stock count).
		/// </summary>
		Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount)> GetInventorySummaryDataAsync();
	}
}
