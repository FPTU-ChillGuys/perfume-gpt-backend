using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests
{
	public class OrderReturnRequestResponse
	{
		public Guid Id { get; set; }
		public Guid OrderId { get; set; }
		public Guid CustomerId { get; set; }
		public string? CustomerEmail { get; set; }
		public Guid? ProcessedById { get; set; }
		public Guid? InspectedById { get; set; }

		public string Reason { get; set; } = null!;
		public string? CustomerNote { get; set; }
		public string? StaffNote { get; set; }
		public string? InspectionNote { get; set; }
		public ReturnRequestStatus Status { get; set; }

		public decimal RequestedRefundAmount { get; set; }
		public decimal? ApprovedRefundAmount { get; set; }
		public bool IsRefunded { get; set; }
		public string? VnpTransactionNo { get; set; }
		public bool IsRestocked { get; set; }

		public List<OrderReturnRequestDetailResponse> ReturnDetails { get; set; } = [];
		public List<MediaResponse> ProofImages { get; set; } = [];

		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class OrderReturnRequestDetailResponse
	{
		public Guid Id { get; set; }
		public Guid OrderDetailId { get; set; }
		public int ReturnedQuantity { get; set; }
		public bool? IsRestocked { get; set; }
		public string? InspectionNote { get; set; }
	}
}
