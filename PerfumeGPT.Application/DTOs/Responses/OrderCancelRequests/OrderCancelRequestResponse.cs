using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests
{
	public record OrderCancelRequestResponse
	{
		public Guid Id { get; init; }
		public Guid OrderId { get; init; }
		public Guid RequestedById { get; init; }
		public string? RequestedByEmail { get; init; }
		public Guid? ProcessedById { get; init; }
		
		public required string Reason { get; init; }
		public string? StaffNote { get; init; }
		public CancelRequestStatus Status { get; init; }

		public bool IsRefundRequired { get; init; }
		public decimal? RefundAmount { get; init; }
		public bool IsRefunded { get; init; }
		public string? VnpTransactionNo { get; init; }

		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
