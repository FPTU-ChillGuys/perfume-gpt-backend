namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers
{
	public record SupplierLookupItem
	{
		public int Id { get; init; }
		public required string Name { get; init; }
		public string? Phone { get; init; }
		public string? ContactEmail { get; init; }
	}
}
