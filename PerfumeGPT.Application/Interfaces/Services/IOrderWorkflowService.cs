using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderWorkflowService
	{
		// Đổi tên hàm cho rõ nghĩa
		Task ProcessForwardShippingStatusAsync(Order order, ShippingStatus newShippingStatus, DateTime? deliveredAtUtc = null);
	}
}
