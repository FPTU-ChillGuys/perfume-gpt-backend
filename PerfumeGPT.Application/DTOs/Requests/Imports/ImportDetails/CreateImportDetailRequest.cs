namespace PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails
{
	public record CreateImportDetailRequest
	{
		public Guid VariantId { get; init; }
		public int ExpectedQuantity { get; init; }
		public decimal UnitPrice { get; init; }
	}
}
