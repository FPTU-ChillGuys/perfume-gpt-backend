using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Loyalty
{
	public class LoyaltyTransactionHistoryItemResponse
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public Guid? VoucherId { get; set; }
		public Guid? OrderId { get; set; }
		public LoyaltyTransactionType TransactionType { get; set; }
		public int PointsChanged { get; set; }
		public int AbsolutePoints { get; set; }
		public string Reason { get; set; } = null!;
	}
}
