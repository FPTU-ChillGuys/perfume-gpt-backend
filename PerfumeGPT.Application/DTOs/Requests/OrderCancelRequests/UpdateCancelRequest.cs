using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public class UpdateCancelRequest
	{
		public CancelOrderReason Reason { get; set; }
		public string? StaffNote { get; set; }
		public bool IsRefundRequired { get; set; }
		public decimal? RefundAmount { get; set; }

		public string? RefundBankName { get; set; }
		public string? RefundAccountNumber { get; set; }
		public string? RefundAccountName { get; set; }
	}
}