namespace PerfumeGPT.Application.DTOs.Responses.Loyalty
{
	public class GetLoyaltyPointResponse
	{
		public Guid UserId { get; set; }
		public int PointBalance { get; set; }
	}
}
