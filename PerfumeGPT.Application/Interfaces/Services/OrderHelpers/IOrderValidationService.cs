using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderValidationService
	{
		BaseResponse<bool> ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus);
	}
}
