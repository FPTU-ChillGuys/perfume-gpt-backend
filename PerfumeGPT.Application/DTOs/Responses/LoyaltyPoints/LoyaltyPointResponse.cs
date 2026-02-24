namespace PerfumeGPT.Application.DTOs.Responses.LoyaltyPoints
{
	public class LoyaltyPointResponse
	{
		public Guid Id { get; set; }
		public int Points { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
