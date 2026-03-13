namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class RedeemVoucherRequest
	{
		public Guid VoucherId { get; set; }
		public string? ReceiverEmailOrPhone { get; set; }
	}
}
