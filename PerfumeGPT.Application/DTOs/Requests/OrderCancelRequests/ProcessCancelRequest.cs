using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public record ProcessCancelRequest
	{
		public bool IsApproved { get; init; }
		public string? StaffNote { get; init; }
		public PaymentMethod? RefundMethod { get; init; }
		public string? ManualTransactionReference { get; set; }
	}
}
