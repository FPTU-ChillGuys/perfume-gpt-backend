using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		Task<BaseResponse<bool>> ReserveStockForOrderAsync(
			Guid orderId,
			List<(Guid VariantId, int Quantity)> items,
			DateTime expiresAt);
		Task<BaseResponse<bool>> CommitReservationAsync(Guid orderId);
		Task<BaseResponse<bool>> ReleaseReservationAsync(Guid orderId);
		Task<BaseResponse<int>> ProcessExpiredReservationsAsync();
	}
}
