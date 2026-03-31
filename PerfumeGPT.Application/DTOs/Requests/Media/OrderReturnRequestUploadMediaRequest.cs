using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class OrderReturnRequestUploadMediaRequest
	{
       public List<IFormFile> Videos { get; set; } = [];
	}
}
