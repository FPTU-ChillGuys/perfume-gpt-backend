using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record UpdateOrderStatusRequest
	{
		public OrderStatus Status { get; init; }
		public string? Note { get; init; }
	}
}
