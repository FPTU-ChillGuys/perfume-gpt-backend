using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class ReviewUploadMediaRequest
	{
		// Chỉ cần danh sách file, vì khách hàng chỉ muốn "Chọn ảnh và Gửi"
		public List<IFormFile> Images { get; set; } = [];
	}
}
