namespace PerfumeGPT.Domain.Enums
{
	public enum ReturnOrderReason
	{
		DamagedProduct = 1,
		WrongItemReceived,
		ItemNotAsDescribed,
		ChangedMind,
		AllergicReaction,
		InsufficientStock,
		ReturnPeriodExpired,      // Quá thời hạn hỗ trợ đổi trả (VD: Quá 7 ngày)
		ProductUsedOrUnsealed,    // Sản phẩm đã bị bóc seal / Đã qua sử dụng
		CustomerDamage,           // Hư hỏng, bể vỡ do lỗi từ phía khách hàng
		InsufficientEvidence,     // Không cung cấp đủ bằng chứng (VD: Không có video đồng kiểm/unbox)
		MissingAccessories,       // Gửi trả thiếu vỏ hộp, phụ kiện hoặc quà tặng kèm
		NonReturnableItem,        // Sản phẩm thuộc danh mục không hỗ trợ đổi trả (Hàng Clearance/Sale lớn)
		SuspectedFraud,           // Nghi ngờ gian lận (Phát hiện tráo hàng giả, hàng fake)

		// --- KHÁC ---
		Other = 99                // Lý do khác (Bắt buộc nhân viên nhập Ghi chú chi tiết)
	}
}
