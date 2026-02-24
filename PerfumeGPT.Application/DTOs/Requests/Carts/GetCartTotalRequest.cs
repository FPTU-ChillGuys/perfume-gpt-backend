namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public class GetCartTotalRequest
	{
		public string? VoucherCode { get; set; }
		public int? DistrictId { get; set; }
		public string? WardCode { get; set; }
	}
}
