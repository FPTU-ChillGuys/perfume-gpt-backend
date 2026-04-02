namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers
{
	public record UpdateSupplierRequest
	{
		public required string Name { get; init; }
		public required string ContactEmail { get; init; }
		public required string Phone { get; init; }
		public required string Address { get; init; }
	}
}
