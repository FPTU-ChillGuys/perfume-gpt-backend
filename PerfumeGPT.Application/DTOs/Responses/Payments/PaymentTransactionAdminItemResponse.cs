using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Payments
{
	public record PaymentTransactionAdminItemResponse
	{
		public Guid Id { get; init; }
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public PaymentMethod Method { get; init; }
		public TransactionType TransactionType { get; init; }
		public TransactionStatus TransactionStatus { get; init; }
		public decimal Amount { get; init; }
		public string? GatewayTransactionNo { get; init; }
		public string? FailureReason { get; init; }
		public Guid? OriginalPaymentId { get; init; }
		public int RetryAttempt { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
