using Microsoft.AspNetCore.Http;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record ImageUploadItem
	{
		public IFormFile? File { get; init; }
		public EntityType EntityType { get; init; }
		public int DisplayOrder { get; init; }
		public bool IsPrimary { get; init; }
		public string? AltText { get; init; }
	}
}
