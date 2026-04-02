namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public record ExcelTemplateResponse
	{
		public required byte[] FileContent { get; init; }
		public required string FileName { get; init; }
		public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
	}
}
