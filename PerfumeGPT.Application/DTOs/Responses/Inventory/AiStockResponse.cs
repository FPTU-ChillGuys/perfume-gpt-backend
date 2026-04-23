using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public record AiStockResponse : StockResponse
	{
		public VariantType Type { get; init; }
		public int ReservedQuantity { get; init; }
	}
}
