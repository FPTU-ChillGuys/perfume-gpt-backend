using PerfumeGPT.Application.DTOs.Requests.Carts;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record CreateInStoreOrderRequest
	{
		public required List<PosScanItemRequest> ScannedItems { get; init; }
		public string? VoucherCode { get; init; }
		public Guid? CustomerId { get; init; } // Rất quan trọng nếu muốn tích điểm hoặc lưu lịch sử

		public bool IsPickupInStore { get; init; } = true;
		public ContactAddressInformation? Recipient { get; init; }
		public required PaymentInformation Payment { get; init; }
		public decimal? ExpectedTotalPrice { get; init; } // Để chống sai lệch giá lúc nhân viên bấm thanh toán

		public string? PosSessionId { get; set; }
	}
}
