using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public record GetCartItemResponse
	{
		public Guid CartItemId { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public required string ImageUrl { get; init; }
		public int VolumeMl { get; init; }
		public VariantType Type { get; init; }
		public decimal VariantPrice { get; init; }
		public int Quantity { get; init; }
		public bool IsAvailable { get; init; }
		public decimal SubTotal => VariantPrice * Quantity;
	}
}