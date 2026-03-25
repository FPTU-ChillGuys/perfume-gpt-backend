using Microsoft.AspNetCore.Http;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record ImageUploadItem(
	IFormFile? File,
	EntityType EntityType,
	int DisplayOrder,
	bool IsPrimary,
	string? AltText);
}
