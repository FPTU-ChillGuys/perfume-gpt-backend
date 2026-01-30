namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class BulkUploadMediaRequest
	{
		public List<SingleImageUploadRequest> Images { get; set; } = [];
	}
}
