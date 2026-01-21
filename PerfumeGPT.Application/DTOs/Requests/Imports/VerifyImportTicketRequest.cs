namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class VerifyImportTicketRequest
	{
		public Guid ImportTicketId { get; set; }
		public List<VerifyImportDetailRequest> ImportDetails { get; set; } = [];
	}

	public class VerifyImportDetailRequest
	{
		public Guid ImportDetailId { get; set; }
		public List<CreateBatchRequest> Batches { get; set; } = [];
	}

	public class CreateBatchRequest
	{
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int Quantity { get; set; }
	}
}
