namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public record GetVariantByInfoRequest
	{
		public string? Barcode { get; init; }
		public string? Sku { get; init; }
		public string? Name { get; init; }
	}
}
