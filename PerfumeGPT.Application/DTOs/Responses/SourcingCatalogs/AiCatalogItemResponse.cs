using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs
{
	/// <summary>
	/// Response cho AI backend qua NATS (không kế thừa để tránh lỗi serialize)
	/// </summary>
	public class AiCatalogItemResponse
	{
		[JsonPropertyName("id")]
		public Guid Id { get; set; }

		[JsonPropertyName("productVariantId")]
		public Guid ProductVariantId { get; set; }

		[JsonPropertyName("supplierId")]
		public int SupplierId { get; set; }

		[JsonPropertyName("supplierName")]
		public string SupplierName { get; set; } = string.Empty;

		[JsonPropertyName("variantSku")]
		public string VariantSku { get; set; } = string.Empty;

		[JsonPropertyName("variantName")]
		public string VariantName { get; set; } = string.Empty;

		[JsonPropertyName("primaryImageUrl")]
		public string? PrimaryImageUrl { get; set; }

		[JsonPropertyName("negotiatedPrice")]
		public decimal NegotiatedPrice { get; set; }

		[JsonPropertyName("estimatedLeadTimeDays")]
		public int EstimatedLeadTimeDays { get; set; }

		[JsonPropertyName("isPrimary")]
		public bool IsPrimary { get; set; }

		[JsonPropertyName("createdAt")]
		public DateTime CreatedAt { get; set; }

		[JsonPropertyName("updatedAt")]
		public DateTime? UpdatedAt { get; set; }

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
