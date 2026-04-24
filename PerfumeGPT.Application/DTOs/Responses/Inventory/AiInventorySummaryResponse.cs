namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	/// <summary>
	/// Response cho AI backend qua NATS (kế thừa từ InventorySummaryResponse)
	/// </summary>
	public record AiInventorySummaryResponse : InventorySummaryResponse
	{
		/// <summary>
		/// Tổng số SKU (tương đương TotalVariants)
		/// </summary>
		public int TotalSku { get; init; }

		/// <summary>
		/// Số SKU sắp hết hoặc hết hàng (tương đương LowStockVariantsCount)
		/// </summary>
		public int LowStockSku { get; init; }

		/// <summary>
		/// Số SKU hết hàng hoàn toàn (tương đương OutOfStockVariantsCount)
		/// </summary>
		public int OutOfStockSku { get; init; }

		/// <summary>
		/// Số lô hàng đã hết hạn (tương đương ExpiredBatchesCount)
		/// </summary>
		public int ExpiredBatches { get; init; }

		/// <summary>
		/// Số lô hàng cận hạn (tương đương ExpiringSoonCount)
		/// </summary>
		public int NearExpiryBatches { get; init; }

		/// <summary>
		/// Số cảnh báo nghiêm trọng (tổng LowStock + OutOfStock)
		/// </summary>
		public int CriticalAlerts { get; init; }

		/// <summary>
		/// Factory method để tạo từ InventorySummaryResponse
		/// </summary>
		public static AiInventorySummaryResponse FromInventorySummary(InventorySummaryResponse response)
		{
			return new AiInventorySummaryResponse
			{
				TotalVariants = response.TotalVariants,
				TotalStockQuantity = response.TotalStockQuantity,
				LowStockVariantsCount = response.LowStockVariantsCount,
				OutOfStockVariantsCount = response.OutOfStockVariantsCount,
				TotalBatches = response.TotalBatches,
				ExpiredBatchesCount = response.ExpiredBatchesCount,
				ExpiringSoonCount = response.ExpiringSoonCount,
				// AI-specific fields
				TotalSku = response.TotalVariants,
				LowStockSku = response.LowStockVariantsCount,
				OutOfStockSku = response.OutOfStockVariantsCount,
				ExpiredBatches = response.ExpiredBatchesCount,
				NearExpiryBatches = response.ExpiringSoonCount,
				CriticalAlerts = response.LowStockVariantsCount + response.OutOfStockVariantsCount
			};
		}
	}
}
