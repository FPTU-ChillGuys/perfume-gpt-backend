using PerfumeGPT.Application.DTOs.Commons;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

namespace PerfumeGPT.Application.Services.Helpers
{
	public static class SellableStockContextLoader
	{
		/// <param name="clearanceScopedVariantIds">
		/// Rỗng: lấy mọi lô đang gắn promotion clearance (dùng cho danh sách sản phẩm / quét barcode không biết variant trước).
		/// Không rỗng: chỉ promotion clearance của các biến thể đó (khớp StockReservationService).
		/// </param>
		public static async Task<SellableStockQueryContext> LoadAsync(
			IUnitOfWork unitOfWork,
			IReadOnlyList<Guid>? clearanceScopedVariantIds = null)
		{
			var policy = await unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var now = DateTime.UtcNow;
			var normalAfter = policy != null ? now.AddDays(policy.StopSellingBeforeExpiryDays) : now;
			var clearanceAfter = policy != null ? now.AddDays(policy.ClearanceBufferDays) : now;

			var scope = clearanceScopedVariantIds?
				.Where(id => id != Guid.Empty)
				.Distinct()
				.ToList() ?? [];

			var clearanceBatchIds = await unitOfWork.PromotionItems.GetClearanceBatchIdsForSellableStockAsync(scope);

			return new SellableStockQueryContext(normalAfter, clearanceAfter, clearanceBatchIds);
		}
	}
}
