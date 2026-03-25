using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public class ManualChangeRequest
	{
		public LoyaltyTransactionType TransactionType { get; set; }
		public int Points { get; set; }
		public string Reason { get; set; } = string.Empty;
	}
}
