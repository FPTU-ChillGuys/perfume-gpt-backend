using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public record ManualChangeRequest
	{
		public Guid UserId { get; init; }
		public LoyaltyTransactionType TransactionType { get; init; }
		public int Points { get; init; }
		public required string Reason { get; init; }
	}
}
