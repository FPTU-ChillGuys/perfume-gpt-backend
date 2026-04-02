using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public record CreateImportTicketRequest
	{
		public required List<CreateImportDetailRequest> ImportDetails { get; init; }
		public int SupplierId { get; init; }
		public DateTime ExpectedArrivalDate { get; init; }
	}
}
