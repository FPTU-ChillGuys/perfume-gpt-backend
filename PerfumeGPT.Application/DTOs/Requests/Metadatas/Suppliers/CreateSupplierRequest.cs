namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers
{
	public class CreateSupplierRequest
	{
		public string Name { get; set; } = null!;
		public string ContactEmail { get; set; } = null!;
		public string Phone { get; set; } = null!;
		public string Address { get; set; } = null!;
	}
}
