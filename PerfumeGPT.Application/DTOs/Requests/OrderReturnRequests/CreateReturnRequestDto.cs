using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record CreateReturnRequestDto
	{
		public Guid OrderId { get; init; }
		public required ReturnOrderReason Reason { get; init; }
		public string? CustomerNote { get; init; }
		public Guid? SavedAddressId { get; init; }
		public ContactAddressInformation? Recipient { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}
