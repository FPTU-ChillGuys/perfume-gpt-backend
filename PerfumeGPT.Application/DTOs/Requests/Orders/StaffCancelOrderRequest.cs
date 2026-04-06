using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record StaffCancelOrderRequest
	{
		public CancelOrderReason Reason { get; init; }
		public string? Note { get; init; }
	}
}
