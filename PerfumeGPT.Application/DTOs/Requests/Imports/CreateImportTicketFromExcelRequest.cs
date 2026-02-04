using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Imports
{
	public class CreateImportTicketFromExcelRequest
	{
		public IFormFile ExcelFile { get; set; } = null!;
		public int SupplierId { get; set; }
		public DateTime ExpectedArrivalDate { get; set; }
	}
}
