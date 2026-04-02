namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record CalculateShippingFeeRequest
	{
		public int ToDistrictId { get; init; }
		public required string ToWardCode { get; init; }
		public int Length { get; init; }
		public int Width { get; init; }
		public int Height { get; init; }
		public int Weight { get; init; }
	}
}
