using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IMediaService
	{
		Task<BaseResponse<MediaResponse>> UploadMediaAsync(Stream fileStream, string fileName, EntityType entityType, Guid entityId, string? altText = null, int displayOrder = 0, bool isPrimary = false);
		Task<BaseResponse<string>> DeleteMediaAsync(Guid mediaId);
		Task<BaseResponse<string>> SetPrimaryMediaAsync(Guid mediaId);
		Task<BaseResponse<List<MediaResponse>>> GetMediaByEntityAsync(EntityType entityType, Guid entityId);
		Task<BaseResponse<MediaResponse?>> GetPrimaryMediaAsync(EntityType entityType, Guid entityId);
		Task<BaseResponse<string>> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId);
	}
}
