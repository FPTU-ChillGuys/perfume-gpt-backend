using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record UserCancelOrderRequest
	{
		public CancelOrderReason Reason { get; init; }
	}
}
