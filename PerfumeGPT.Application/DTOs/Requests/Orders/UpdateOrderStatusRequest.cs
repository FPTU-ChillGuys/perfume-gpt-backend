namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record UpdateOrderStatusRequest
	{
		public string? Note { get; init; }
	}
}
