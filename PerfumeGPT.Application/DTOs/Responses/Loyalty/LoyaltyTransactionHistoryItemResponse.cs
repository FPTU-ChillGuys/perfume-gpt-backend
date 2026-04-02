using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Loyalty
{
	public record LoyaltyTransactionHistoryItemResponse
	{
		public Guid Id { get; init; }
		public Guid UserId { get; init; }
		public Guid? VoucherId { get; init; }
		public Guid? OrderId { get; init; }
		public LoyaltyTransactionType TransactionType { get; init; }
		public int PointsChanged { get; init; }
		public int AbsolutePoints { get; init; }
		public required string Reason { get; init; }
	}
}
