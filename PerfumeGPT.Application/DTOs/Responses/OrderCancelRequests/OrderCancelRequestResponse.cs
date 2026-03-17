using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests
{
	public class OrderCancelRequestResponse
	{
		public Guid Id { get; set; }
		public Guid OrderId { get; set; }
		public Guid RequestedById { get; set; }
		public string? RequestedByEmail { get; set; }
		public Guid? ProcessedById { get; set; }
		
		public string Reason { get; set; } = null!;
		public string? StaffNote { get; set; }
		public CancelRequestStatus Status { get; set; }

		public bool IsRefundRequired { get; set; }
		public decimal? RefundAmount { get; set; }
		public bool IsRefunded { get; set; }
		public string? VnpTransactionNo { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
