using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record StaffCancelOrderRequest
	{
		public CancelOrderReason Reason { get; init; }
		public string? Note { get; init; }
		public string? RefundBankName { get; init; }
		public string? RefundAccountNumber { get; init; }
		public string? RefundAccountName { get; init; }
	}
}
