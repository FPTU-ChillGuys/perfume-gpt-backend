using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record ProcessInStoreReturnFastTrackDto
	{
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public required ReturnOrderReason Reason { get; init; }
		public bool IsRefundOnly { get; init; }
		public required List<ReturnItemDto> ReturnItems { get; init; }

		public decimal ApprovedRefundAmount { get; init; }
		public bool IsRestocked { get; init; }
		public string? InspectionNote { get; init; }

		public string? CustomerNote { get; init; }
		public string? RefundBankName { get; init; }
		public string? RefundAccountNumber { get; init; }
		public string? RefundAccountName { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}
