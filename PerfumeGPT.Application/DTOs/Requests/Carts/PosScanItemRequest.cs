using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public record PosScanItemRequest
	{
		public required string Barcode { get; init; }
		public required string BatchCode { get; init; }
		public int Quantity { get; init; } = 1;
	}

	public record PreviewPosOrderRequest
	{
		public required List<PosScanItemRequest> ScannedItems { get; init; }
		public string? VoucherCode { get; init; }
		public Guid? CustomerId { get; init; }
		public string? SessionId { get; init; }
		public ContactAddressInformation? Recipient { get; init; }
	}
}
