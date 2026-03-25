namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers
{
	public class SupplierResponse
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public string ContactEmail { get; set; } = null!;
		public string Phone { get; set; } = null!;
		public string Address { get; set; } = null!;
	}
}
