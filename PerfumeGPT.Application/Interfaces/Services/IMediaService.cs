using PerfumeGPT.Application.DTOs.Requests.Media;
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

	// Temporary media methods
	Task<BaseResponse<List<TemporaryMediaResponse>>> UploadTemporaryMediaAsync(Guid? userId, ReviewUploadMediaRequest request, EntityType targetEntityType = EntityType.Review);
	Task<BaseResponse<List<TemporaryMediaResponse>>> UploadTemporaryProductMediaAsync(Guid? userId, ProductUploadMediaRequest request);
	Task<BaseResponse<TemporaryMediaResponse>> UploadTemporaryVariantMediaAsync(Guid? userId, VariantUploadMediaRequest request);
	Task<BaseResponse<string>> DeleteTemporaryMediaAsync(Guid temporaryMediaId);
	Task<BaseResponse<List<TemporaryMediaResponse>>> GetUserTemporaryMediaAsync(Guid userId);
	}
}
