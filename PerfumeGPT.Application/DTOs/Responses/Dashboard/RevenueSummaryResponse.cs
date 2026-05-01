using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public record PaymentMethodDistributionResponse
	{
		public PaymentMethod PaymentMethod { get; init; }
		public int TransactionsCount { get; init; }
		public decimal Amount { get; init; }
	}

	public record RevenueSummaryResponse
	{
		public DateTime FromDate { get; init; }
		public DateTime ToDate { get; init; }
		public decimal GrossRevenue { get; init; }
		public decimal RefundedAmount { get; init; }
		public decimal NetRevenue { get; init; }
		public int SuccessfulTransactionsCount { get; init; }
		public int PaidOrdersCount { get; init; }
		public List<PaymentMethodDistributionResponse> PaymentMethodDistribution { get; init; } = [];
	}
}
