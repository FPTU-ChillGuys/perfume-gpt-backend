namespace PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs
{
	/// <summary>
	/// Response cho AI backend qua NATS (kế thừa từ CatalogItemResponse)
	/// </summary>
	public record AiCatalogItemResponse : CatalogItemResponse
	{
		/// <summary>
		/// Factory method để tạo từ CatalogItemResponse
		/// </summary>
		public static AiCatalogItemResponse FromCatalogItemResponse(CatalogItemResponse response)
		{
			return new AiCatalogItemResponse
			{
				Id = response.Id,
				ProductVariantId = response.ProductVariantId,
				SupplierId = response.SupplierId,
				SupplierName = response.SupplierName,
				VariantSku = response.VariantSku,
				VariantName = response.VariantName,
				PrimaryImageUrl = response.PrimaryImageUrl,
				NegotiatedPrice = response.NegotiatedPrice,
				EstimatedLeadTimeDays = response.EstimatedLeadTimeDays,
				IsPrimary = response.IsPrimary,
				CreatedAt = response.CreatedAt,
				UpdatedAt = response.UpdatedAt
			};
		}
	}
}
