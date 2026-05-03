using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IPromotionItemRepository : IGenericRepository<PromotionItem>
	{
		Task<List<PromotionItem>> GetActiveByCampaignIdsAsync(IEnumerable<Guid> campaignIds);
		Task<List<PromotionItem>> GetActiveClearancePromotionsByVariantIdAsync(Guid variantId, DateTime now);
		/// <summary>
		/// Id lô đang thuộc promotion clearance active (khớp điều kiện StockReservationService). Danh sách variant rỗng = toàn hệ thống.
		/// </summary>
		Task<HashSet<Guid>> GetClearanceBatchIdsForSellableStockAsync(IReadOnlyList<Guid> scopedVariantIds);
	}
}
