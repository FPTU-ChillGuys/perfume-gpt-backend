namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record RedeemVoucherRequest
	{
		public Guid VoucherId { get; init; }
		public string? ReceiverEmailOrPhone { get; init; }
	}
}
