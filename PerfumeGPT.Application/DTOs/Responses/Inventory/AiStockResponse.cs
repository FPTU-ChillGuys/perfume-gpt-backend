using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	/// <summary>
	/// Response cho AI backend qua NATS (kế thừa từ StockResponse)
	/// </summary>
	public record AiStockResponse : StockResponse
	{
		/// <summary>
		/// Loại sản phẩm (mặc định là Standard)
		/// </summary>
		public string Type { get; init; } = "Standard";

		/// <summary>
		/// Số lượng đã đặt giữ (tính từ AvailableQuantity)
		/// </summary>
		public int ReservedQuantity { get; init; }

		/// <summary>
		/// Factory method để tạo từ StockResponse
		/// </summary>
		public static AiStockResponse FromStockResponse(StockResponse response)
		{
			return new AiStockResponse
			{
				Id = response.Id,
				VariantId = response.VariantId,
				VariantSku = response.VariantSku,
				ProductName = response.ProductName,
				VariantImageUrl = response.VariantImageUrl,
				ReplenishmentPolicy = response.ReplenishmentPolicy,
				VariantStatus = response.VariantStatus,
				VolumeMl = response.VolumeMl,
				ConcentrationName = response.ConcentrationName,
				TotalQuantity = response.TotalQuantity,
				AvailableQuantity = response.AvailableQuantity,
				LowStockThreshold = response.LowStockThreshold,
				BasePrice = response.BasePrice,
				Status = response.Status,
				// AI-specific fields
				Type = "Standard",
				ReservedQuantity = Math.Max(0, response.TotalQuantity - response.AvailableQuantity)
			};
		}
	}
}
