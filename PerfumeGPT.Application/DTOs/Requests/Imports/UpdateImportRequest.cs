using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public record UpdateImportRequest
	{
		public required List<UpdateImportDetailRequest> ImportDetails { get; init; }
		public int SupplierId { get; init; }
		public DateTime ExpectedArrivalDate { get; init; }
	}
}
