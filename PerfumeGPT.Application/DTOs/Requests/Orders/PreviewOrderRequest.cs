namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record PreviewOrderRequest
	{
		public required List<string> BarCodes { get; init; }
		public required string WardCode { get; init; }
		public int DistrictId { get; init; }
		public string? VoucherCode { get; init; }
	}
}
