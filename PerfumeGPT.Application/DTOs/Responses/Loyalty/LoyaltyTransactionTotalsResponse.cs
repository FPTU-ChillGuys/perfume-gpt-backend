namespace PerfumeGPT.Application.DTOs.Responses.Loyalty
{
	public class LoyaltyTransactionTotalsResponse
	{
		public Guid UserId { get; set; }
		public int TotalEarnedPoints { get; set; }
		public int TotalSpentPoints { get; set; }
		public int PointBalance { get; set; }
		public int TotalTransactions { get; set; }
	}
}
