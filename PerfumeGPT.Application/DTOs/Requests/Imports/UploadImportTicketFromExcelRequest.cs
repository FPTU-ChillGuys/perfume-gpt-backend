using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public record UploadImportTicketFromExcelRequest
	{
		public required IFormFile ExcelFile { get; init; }
		public DateTime ExpectedArrivalDate { get; init; }
	}
}
