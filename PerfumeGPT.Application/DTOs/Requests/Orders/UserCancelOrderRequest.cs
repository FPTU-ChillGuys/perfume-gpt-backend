using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record UserCancelOrderRequest
	{
		public CancelOrderReason Reason { get; init; }
		public string? RefundBankName { get; init; }
		public string? RefundAccountNumber { get; init; }
		public string? RefundAccountName { get; init; }
	}
}
