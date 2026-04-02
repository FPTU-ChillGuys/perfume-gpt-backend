namespace PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails
{
	public record UpdateImportDetailRequest
	{
		public Guid? Id { get; init; }
		public Guid VariantId { get; init; }
		public int ExpectedQuantity { get; init; }
		public decimal UnitPrice { get; init; }
	}
}
