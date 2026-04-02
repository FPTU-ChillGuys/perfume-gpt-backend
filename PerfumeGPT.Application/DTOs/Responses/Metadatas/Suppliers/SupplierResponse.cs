namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers
{
	public record SupplierResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
		public required string ContactEmail { get; init; }
		public required string Phone { get; init; }
		public required string Address { get; init; }
	}
}
