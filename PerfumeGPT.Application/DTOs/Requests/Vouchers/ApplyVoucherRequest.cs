namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class ApplyVoucherRequest
	{
		public Guid VoucherId { get; set; }
		public decimal OrderAmount { get; set; }
	}
}
