namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class PreviewOrderRequest
	{
		public List<string> BarCodes { get; set; } = new List<string>();
		public string WardCode { get; set; } = string.Empty;
		public int DistrictId { get; set; }
		public string? VoucherCode { get; set; }
	}
}
