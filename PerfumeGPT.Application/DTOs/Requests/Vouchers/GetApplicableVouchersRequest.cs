namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record ApplicableVoucherCartItemRequest
	{
		public Guid VariantId { get; init; }
		public int Quantity { get; init; }
		public decimal Price { get; init; }
	}

	public record GetApplicableVouchersRequest
	{
		public required List<ApplicableVoucherCartItemRequest> CartItems { get; init; }
	}
}
