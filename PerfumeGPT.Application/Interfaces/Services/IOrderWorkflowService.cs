using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderWorkflowService
	{
		Task ProcessForwardShippingStatusAsync(Order order, ShippingStatus newShippingStatus, DateTime? deliveredAtUtc = null);
	}
}
