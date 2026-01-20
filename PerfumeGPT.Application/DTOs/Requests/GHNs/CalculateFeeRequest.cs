namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public class CalculateFeeRequest
	{
		public int ToDistrictId { get; set; }
		public string ToWardCode { get; set; } = null!;
		public int Length { get; set; } = 30; // Assume default value
		public int Width { get; set; } = 40; // Assume default value
		public int Height { get; set; } = 20; // Assume default value
		public int Weight { get; set; } = 3000; // Assume default value

		//public List<Items> Items { get; set; } = new();
	}
}
