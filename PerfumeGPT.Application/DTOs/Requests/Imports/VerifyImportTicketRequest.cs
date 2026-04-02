using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public record VerifyImportTicketRequest
	{
		public required List<VerifyImportDetailRequest> ImportDetails { get; init; }
	}
}
