namespace PerfumeGPT.Application.DTOs.Responses.Loyalty
{
	public record LoyaltyTransactionTotalsResponse
	{
		public Guid UserId { get; init; }
		public int TotalEarnedPoints { get; init; }
		public int TotalSpentPoints { get; init; }
		public int PointBalance { get; init; }
		public int TotalTransactions { get; init; }
	}
}
