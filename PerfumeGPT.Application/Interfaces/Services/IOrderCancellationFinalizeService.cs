using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	/// <summary>
	/// Chuỗi side-effect chung khi hủy đơn: promotion, trạng thái đơn, thanh toán pending, vận chuyển, tồn kho, voucher.
	/// </summary>
	public interface IOrderCancellationFinalizeService
	{
		/// <returns>Mã vận đơn (nếu có) để enqueue hủy với đối tác vận chuyển.</returns>
		Task<string?> FinalizeOrderCancellationAsync(Order order, CancelOrderReason cancelReason);
	}
}
