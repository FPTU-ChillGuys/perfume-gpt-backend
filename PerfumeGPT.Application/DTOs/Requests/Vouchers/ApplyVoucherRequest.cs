namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class ApplyVoucherRequest
	{
		public string VoucherCode { get; set; } = null!;
		public decimal OrderAmount { get; set; }
	}
}
