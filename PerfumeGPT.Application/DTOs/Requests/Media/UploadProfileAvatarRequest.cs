using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class UploadProfileAvatarRequest
	{
		[Required(ErrorMessage = "Avatar image is required")]
		public IFormFile Avatar { get; set; } = null!;

		public string? AltText { get; set; }
	}
}
