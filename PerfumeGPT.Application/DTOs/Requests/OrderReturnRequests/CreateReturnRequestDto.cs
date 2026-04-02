using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record CreateReturnRequestDto
	{
		public Guid OrderId { get; init; }
		public required ReturnOrderReason Reason { get; init; }
		public decimal RequestedRefundAmount { get; init; }
		public string? CustomerNote { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}
