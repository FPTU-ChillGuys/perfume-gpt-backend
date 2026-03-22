namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public class RevenueSummaryResponse
	{
		public DateTime FromDate { get; set; }
		public DateTime ToDate { get; set; }
		public decimal GrossRevenue { get; set; }
		public decimal RefundedAmount { get; set; }
		public decimal NetRevenue { get; set; }
		public int SuccessfulTransactionsCount { get; set; }
		public int PaidOrdersCount { get; set; }
	}
}
