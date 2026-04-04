using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Momos
{
	public record MomoRefundResponse
	{
		public bool IsSuccess { get; init; }
		public required string Message { get; init; }
		public Guid PaymentId { get; init; }
		public string? ResultCode { get; init; }
		public string? TransactionNo { get; init; }
		public decimal Amount { get; init; }
		public PaymentMethod Method { get; init; } = PaymentMethod.Momo;
	}
}
