using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class UpdateImportTicketRequest
	{
		public ImportStatus Status { get; set; }
	}

	public class UpdateFullImportTicketRequest
	{
		public int SupplierId { get; set; }
		public DateTime ExpectedArrivalDate { get; set; }
		public List<UpdateImportDetailRequest> ImportDetails { get; set; } = [];
	}

	public class UpdateImportDetailRequest
	{
		public Guid? Id { get; set; }
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
	}
}
