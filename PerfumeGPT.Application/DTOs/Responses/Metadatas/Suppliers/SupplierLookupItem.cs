namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers
{
	public class SupplierLookupItem
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public string? Phone { get; set; }
		public string? ContactEmail { get; set; }
	}
}
