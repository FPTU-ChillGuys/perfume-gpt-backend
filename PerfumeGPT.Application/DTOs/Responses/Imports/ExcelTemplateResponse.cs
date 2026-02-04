namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public class ExcelTemplateResponse
	{
		public byte[] FileContent { get; set; } = null!;
		public string FileName { get; set; } = null!;
		public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
	}
}
