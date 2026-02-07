using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.ImportDetails
{
	public class CreateImportDetailRequest
	{
		[JsonIgnore]
		public Guid TicketId { get; set; }
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
	}
}
