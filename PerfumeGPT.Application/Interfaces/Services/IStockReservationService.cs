using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		/// <summary>
		/// Tổng số lượng có thể bán theo lô (sau buffer hết hạn + clearance), khớp với <see cref="ReserveStockForOrderAsync"/>.
		/// </summary>
		Task<IReadOnlyDictionary<Guid, int>> GetAggregatedSellableBatchAvailableByVariantsAsync(IReadOnlyList<Guid> variantIds);

		/// <summary>
		/// Tổng tồn theo lô chỉ tính lô thỏa ExpiryDate &gt; mốc ngừng bán thông thường (không cộng phần vượt buffer của lô xả kho).
		/// </summary>
		Task<IReadOnlyDictionary<Guid, int>> GetSafeDateOnlySellableBatchAvailableByVariantsAsync(IReadOnlyList<Guid> variantIds);

		/// <summary>
		/// Mốc ExpiryDate tối thiểu (so với UtcNow + buffer) cho lô thường và lô clearance, cùng tập Id lô đang xả kho.
		/// </summary>
		Task<(DateTime NormalSellableAfterUtc, DateTime ClearanceSellableAfterUtc, HashSet<Guid> ClearanceBatchIds)> GetReservationBatchSellingWindowsAsync(IReadOnlyList<Guid> variantIds);

		/// <summary>
		/// Tổng hợp tồn bán được theo lô cho một variant: aggregated (clearance + safeDate), chỉ safeDate, và cờ có clearance active.
		/// </summary>
		Task<(int AggregatedSellable, int SafeDateOnlySellable, bool HasClearanceBypass)> GetVariantSellableSnapshotForCartAsync(Guid variantId);

		Task ReserveStockForOrderAsync(Order order);
		Task ReserveExactBatchStockForOrderAsync(Order order);
		Task CommitReservationAsync(Guid orderId);
		Task ReleaseOrRestockCancelledOrderAsync(Guid orderId);
		Task<(int OrdersCleaned, int ReservationsCleaned)> CleanupExpiredOrdersAndReservationsAsync();
	}
}
