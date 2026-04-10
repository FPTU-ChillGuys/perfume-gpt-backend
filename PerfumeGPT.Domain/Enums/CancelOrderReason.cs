namespace PerfumeGPT.Domain.Enums
{
	public enum CancelOrderReason
	{
		ChangedMind = 1,
		FoundBetterPrice,
		WrongShippingInformation,
		PaymentIssue,
		DeliveryTooLate,
		InsufficientStock,
		CustomerRequested,            // Khách hàng yêu cầu hủy (qua Hotline/Tin nhắn)
		SuspectedFraud,               // Nghi ngờ đơn hàng giả mạo / Gian lận
		UnreachableCustomer,          // Không thể liên lạc được với khách hàng
		PaymentTimeout,               // Quá hạn thanh toán (Dùng cho job Auto-cancel hủy cọc)
		PricingOrSystemError,         // Lỗi hệ thống hoặc sai giá sản phẩm
		DamagedOrDefectiveStock,      // Hàng kiểm tra trước khi giao bị lỗi/vỡ
		OutOfServiceArea,             // Khu vực không hỗ trợ giao hàng

		// --- LÝ DO KHÁC ---
		Other = 99                    // Lý do khác
	}
}
