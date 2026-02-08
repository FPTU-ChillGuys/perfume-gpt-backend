using PerfumeGPT.Application.DTOs.Requests.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class CreateImportTicketRequest
	{
		public int SupplierId { get; set; }
		public DateTime ExpectedArrivalDate { get; set; }
		public List<CreateImportDetailRequest> ImportDetails { get; set; } = [];
	}
}
