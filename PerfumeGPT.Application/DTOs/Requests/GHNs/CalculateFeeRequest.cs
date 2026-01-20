namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public class CalculateFeeRequest
	{
		//public List<GetCartItemResponse> CartItemIds { get; set; } 
		//public int FromDistrictId { get; set; }
		//public int FromWardCode { get; set; }
		public int ToDistrictId { get; set; }
		public string ToWardCode { get; set; } = null!;
		public int Length { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int Weight { get; set; }
	}
}
