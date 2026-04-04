using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record ProcessRefundRequest
	{
		public PaymentMethod RefundMethod { get; init; }
	}
}
