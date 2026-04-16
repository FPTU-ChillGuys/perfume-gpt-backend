using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IReturnWorkflowService
	{
		Task ProcessReturnShippingStatusAsync(Order order, OrderReturnRequest returnRequest, ShippingStatus newShippingStatus);
	}
}
