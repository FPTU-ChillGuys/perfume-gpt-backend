using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record UpdateReturnRequestDto
	{
		public required ReturnOrderReason Reason { get; init; }
		public string? CustomerNote { get; init; }
       public List<Guid>? TemporaryMediaIds { get; init; }
		public List<Guid>? RemoveMediaIds { get; init; }
	}
}
