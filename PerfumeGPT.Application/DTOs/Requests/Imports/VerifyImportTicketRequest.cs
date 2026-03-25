using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class VerifyImportTicketRequest
	{
		public List<VerifyImportDetailRequest> ImportDetails { get; set; } = [];
	}
}
