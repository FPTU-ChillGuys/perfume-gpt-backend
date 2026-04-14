namespace PerfumeGPT.Domain.Enums
{
	public enum ReplenishmentPolicy
	{
		AutoRestock = 1,  // AI tự động lên danh sách khi gần hết hàng
		ManualOnly = 2,   // Chỉ khi nào Giám đốc kho duyệt tay mới được nhập (Hàng đắt tiền)
		DoNotRestock = 3  // Hàng thanh lý, hàng limited, cấm nhập thêm
	}
}
