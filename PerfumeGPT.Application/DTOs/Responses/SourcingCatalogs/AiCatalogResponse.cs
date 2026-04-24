namespace PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs
{
	/// <summary>
	/// Response tổng cho AI backend qua NATS (chứa danh sách catalog items)
	/// </summary>
	public class AiCatalogResponse
	{
		public AiCatalogItemResponse[] Items { get; set; } = Array.Empty<AiCatalogItemResponse>();
	}
}
