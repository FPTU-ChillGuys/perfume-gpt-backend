using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	/// <summary>
	/// Example controller showing how to use SupabaseService for image upload
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class ImageUploadController : ControllerBase
	{
		private readonly ISupabaseService _supabaseService;

		public ImageUploadController(ISupabaseService supabaseService)
		{
			_supabaseService = supabaseService;
		}

		/// <summary>
		/// Upload a product variant image
		/// </summary>
		[HttpPost("variant")]
		[ProducesResponseType(typeof(BaseResponse<string>), 200)]
		public async Task<IActionResult> UploadVariantImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest(BaseResponse<string>.Fail("No file uploaded", ResponseErrorType.BadRequest));
			}

			// Validate file type (optional)
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return BadRequest(BaseResponse<string>.Fail("Invalid file type. Only images are allowed.", ResponseErrorType.BadRequest));
			}

			// Validate file size (optional, e.g., max 5MB)
			if (file.Length > 5 * 1024 * 1024)
			{
				return BadRequest(BaseResponse<string>.Fail("File size exceeds 5MB limit.", ResponseErrorType.BadRequest));
			}

			using var stream = file.OpenReadStream();
			var imageUrl = await _supabaseService.UploadVariantImageAsync(stream, file.FileName);

			if (string.IsNullOrWhiteSpace(imageUrl))
			{
				return StatusCode(500, BaseResponse<string>.Fail("Failed to upload image", ResponseErrorType.InternalError));
			}

			return Ok(BaseResponse<string>.Ok(imageUrl, "Image uploaded successfully"));
		}

		/// <summary>
		/// Upload a user avatar image
		/// </summary>
		[HttpPost("avatar")]
		[ProducesResponseType(typeof(BaseResponse<string>), 200)]
		public async Task<IActionResult> UploadAvatarImage(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest(BaseResponse<string>.Fail("No file uploaded", ResponseErrorType.BadRequest));
			}

			// Validate file type
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return BadRequest(BaseResponse<string>.Fail("Invalid file type. Only images are allowed.", ResponseErrorType.BadRequest));
			}

			// Validate file size (e.g., max 2MB for avatars)
			if (file.Length > 2 * 1024 * 1024)
			{
				return BadRequest(BaseResponse<string>.Fail("File size exceeds 2MB limit.", ResponseErrorType.BadRequest));
			}

			using var stream = file.OpenReadStream();
			var imageUrl = await _supabaseService.UploadAvatarImageAsync(stream, file.FileName);

			if (string.IsNullOrWhiteSpace(imageUrl))
			{
				return StatusCode(500, BaseResponse<string>.Fail("Failed to upload image", ResponseErrorType.InternalError));
			}

			return Ok(BaseResponse<string>.Ok(imageUrl, "Avatar uploaded successfully"));
		}

		/// <summary>
		/// Delete a variant image
		/// </summary>
		[HttpDelete("variant")]
		[ProducesResponseType(typeof(BaseResponse<bool>), 200)]
		public async Task<IActionResult> DeleteVariantImage([FromQuery] string imageUrl)
		{
			if (string.IsNullOrWhiteSpace(imageUrl))
			{
				return BadRequest(BaseResponse<bool>.Fail("Image URL is required", ResponseErrorType.BadRequest));
			}

			var result = await _supabaseService.DeleteVariantImageAsync(imageUrl);

			if (!result)
			{
				return StatusCode(500, BaseResponse<bool>.Fail("Failed to delete image", ResponseErrorType.InternalError));
			}

			return Ok(BaseResponse<bool>.Ok(true, "Image deleted successfully"));
		}

		/// <summary>
		/// Delete an avatar image
		/// </summary>
		[HttpDelete("avatar")]
		[ProducesResponseType(typeof(BaseResponse<bool>), 200)]
		public async Task<IActionResult> DeleteAvatarImage([FromQuery] string imageUrl)
		{
			if (string.IsNullOrWhiteSpace(imageUrl))
			{
				return BadRequest(BaseResponse<bool>.Fail("Image URL is required", ResponseErrorType.BadRequest));
			}

			var result = await _supabaseService.DeleteAvatarImageAsync(imageUrl);

			if (!result)
			{
				return StatusCode(500, BaseResponse<bool>.Fail("Failed to delete image", ResponseErrorType.InternalError));
			}

			return Ok(BaseResponse<bool>.Ok(true, "Image deleted successfully"));
		}
	}
}
