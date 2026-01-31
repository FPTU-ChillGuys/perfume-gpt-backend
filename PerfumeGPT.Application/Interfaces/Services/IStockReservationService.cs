using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		/// <summary>
		/// Reserve stock for an order during checkout (marks stock as reserved, doesn't deduct yet)
		/// </summary>
		/// <param name="orderId">The order ID</param>
		/// <param name="items">List of (VariantId, Quantity) to reserve</param>
		/// <param name="expiresAt">When the reservation expires (e.g., payment deadline)</param>
		Task<BaseResponse<bool>> ReserveStockForOrderAsync(
			Guid orderId, 
			List<(Guid VariantId, int Quantity)> items, 
			DateTime expiresAt);

		/// <summary>
		/// Commit reservations and deduct stock when payment is successful
		/// </summary>
		/// <param name="orderId">The order ID</param>
		Task<BaseResponse<bool>> CommitReservationAsync(Guid orderId);

		/// <summary>
		/// Release reservation when payment expires or order is cancelled
		/// </summary>
		/// <param name="orderId">The order ID</param>
		Task<BaseResponse<bool>> ReleaseReservationAsync(Guid orderId);

		/// <summary>
		/// Process all expired reservations (for scheduled job)
		/// </summary>
		Task<BaseResponse<int>> ProcessExpiredReservationsAsync();

		/// <summary>
		/// Get available quantity for a variant (total - reserved)
		/// </summary>
		Task<int> GetAvailableQuantityAsync(Guid variantId);
	}
}
