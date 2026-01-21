namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class CreateImportTicketRequest
	{
		public int SupplierId { get; set; }
		public DateTime ImportDate { get; set; }
		public List<CreateImportDetailRequest> ImportDetails { get; set; } = [];
	}

	public class CreateImportDetailRequest
	{
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
	}
}
