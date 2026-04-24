using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs
{
	/// <summary>
	/// Response tổng cho AI backend qua NATS (chứa danh sách catalog items)
	/// </summary>
	public class AiCatalogResponse
	{
		[JsonPropertyName("catalogs")]
		public AiCatalogItemResponse[] Catalogs { get; set; } = Array.Empty<AiCatalogItemResponse>();
	}
}
