using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.DTOs.Responses.Payments
{
	public record PaymentTransactionOverviewResponse
	{
		public required PaymentTransactionSummaryResponse Summary { get; init; }
		public required PagedResult<PaymentTransactionAdminItemResponse> Transactions { get; init; }
	}

	public record PaymentTransactionSummaryResponse
	{
		public DateTime FromDate { get; init; }
		public DateTime ToDate { get; init; }
		public int TotalTransactions { get; init; }
		public int TotalPaymentTransactions { get; init; }
		public int TotalRefundTransactions { get; init; }
		public int PendingTransactionsCount { get; init; }
		public int SuccessTransactionsCount { get; init; }
		public int FailedTransactionsCount { get; init; }
		public int CancelledTransactionsCount { get; init; }
		public decimal TotalPaymentAmount { get; init; }
     public decimal TotalShippingFeeDeductedPerOrder { get; init; }
		public decimal TotalPaymentAmountExcludingShipping { get; init; }
		public decimal TotalRefundAmount { get; init; }
	}
}
