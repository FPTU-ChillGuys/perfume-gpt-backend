namespace PerfumeGPT.Application.DTOs.Requests.Base
{
	public class FileUpload
	{
		public Stream FileStream { get; set; } = null!;
		public string FileName { get; set; } = null!;
		public long Length { get; set; }
		public string ContentType { get; set; } = null!;
	}
}
