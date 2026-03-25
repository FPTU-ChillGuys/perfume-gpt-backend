using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class UpdateOrderStatusRequest
	{
		public OrderStatus Status { get; set; }
		public string? Note { get; set; }
	}
}
