using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IMediaService
	{
		Task<BaseResponse<string>> DeleteMediaAsync(Guid mediaId);
		Task<BaseResponse<string>> SetPrimaryMediaAsync(Guid mediaId);
		Task<BaseResponse<List<MediaResponse>>> GetMediaByEntityAsync(EntityType entityType, Guid entityId);
		Task<BaseResponse<MediaResponse?>> GetPrimaryMediaAsync(EntityType entityType, Guid entityId);
		Task<BaseResponse<string>> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId);

		// Profile avatar methods
		Task<bool> CreateProfileAvatarFromUrlAsync(Guid userId, string avatarUrl, string? altText = null);
		Task<BaseResponse<string>> UploadProfileAvatarAsync(Guid userId, ProfileAvtarUploadRequest request);
		Task<BaseResponse<string>> DeleteProfileAvatarAsync(Guid userId);

		// Temporary media methods
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadReviewTemporaryMediaAsync(Guid? userId, ReviewUploadMediaRequest request);
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadPageTemporaryMediaAsync(Guid? userId, PageUploadMediaRequest request);
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadProductTemporaryMediaAsync(Guid? userId, ProductUploadMediaRequest request);
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadVariantTemporaryMediaAsync(Guid? userId, VariantUploadMediaRequest request);
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadBannerTemporaryMediaAsync(Guid? userId, BannerUploadMediaRequest request);
		Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadOrderReturnRequestTemporaryMediaAsync(Guid? userId, OrderReturnRequestUploadMediaRequest request);
	}
}
