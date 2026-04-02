using MapsterMapper;
using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.Media;
using static PerfumeGPT.Domain.Entities.TemporaryMedia;

namespace PerfumeGPT.Application.Services
{
	public class MediaService : IMediaService
	{
		#region Dependencies
		private readonly ISupabaseService _supabaseService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
		private static readonly string[] AllowedVideoExtensions = [".mp4", ".mov", ".webm", ".m4v"];
		private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
		private const long MaxVideoFileSize = 100 * 1024 * 1024; // 100MB

		public MediaService(
			ISupabaseService supabaseService,
			IUnitOfWork unitOfWork,
			IMapper mapper)
		{
			_supabaseService = supabaseService;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		#endregion Dependencies

		#region Media CRUD
		public async Task<BaseResponse<List<MediaResponse>>> GetMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaList = await _unitOfWork.Media.GetMediaByEntityTypeAsync(entityType, entityId);
			return BaseResponse<List<MediaResponse>>.Ok(
				_mapper.Map<List<MediaResponse>>(mediaList),
				"Media retrieved successfully");
		}

		public async Task<BaseResponse<MediaResponse?>> GetPrimaryMediaAsync(EntityType entityType, Guid entityId)
		{
			var media = await _unitOfWork.Media.GetPrimaryMediaAsync(entityType, entityId);
			return BaseResponse<MediaResponse?>.Ok(
				media != null ? _mapper.Map<MediaResponse>(media) : null,
				media != null ? "Primary media retrieved successfully" : "No primary media found");
		}

		public async Task<BaseResponse<string>> SetPrimaryMediaAsync(Guid mediaId)
		{
			var media = await _unitOfWork.Media.GetByIdAsync(mediaId);
			if (media == null || media.IsDeleted)
				throw AppException.NotFound("Media not found");

			var existingPrimary = await _unitOfWork.Media.GetPrimaryMediaAsync(media.EntityType, media.EntityId);
			if (existingPrimary != null && existingPrimary.Id != mediaId)
			{
				existingPrimary.UnsetPrimary();
				_unitOfWork.Media.Update(existingPrimary);
			}

			media.SetAsPrimary();
			_unitOfWork.Media.Update(media);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to set primary media");

			return BaseResponse<string>.Ok(mediaId.ToString(), "Primary media set successfully");
		}

		public async Task<BaseResponse<string>> DeleteMediaAsync(Guid mediaId)
		{
			var media = await _unitOfWork.Media.GetByIdAsync(mediaId)
				?? throw AppException.NotFound("Media not found");

			media.EnsureNotPrimary();

			return await DeleteMediaInternalAsync(media, mediaId.ToString(), "Media deleted successfully");
		}

		public async Task<BaseResponse<string>> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaList = await _unitOfWork.Media.GetMediaByEntityTypeAsync(entityType, entityId);
			var bucketName = GetBucketName(entityType);

			foreach (var media in mediaList)
				await _supabaseService.DeleteImageAsync(media.Url, bucketName);

			var count = await _unitOfWork.Media.DeleteAllMediaByEntityAsync(entityType, entityId);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete media");

			return BaseResponse<string>.Ok(count.ToString(), $"{count} media items deleted successfully");
		}
		#endregion Media CRUD

		#region Profile Avatar
		public async Task<bool> CreateProfileAvatarFromUrlAsync(Guid userId, string avatarUrl, string? altText = null)
		{
			if (string.IsNullOrWhiteSpace(avatarUrl)) return false;

			var existingMedia = await _unitOfWork.Media.GetPrimaryMediaAsync(EntityType.User, userId);
			var mimeType = GetMimeTypeFromUrl(avatarUrl);

			if (existingMedia != null)
			{
				var fileMetadata = new FileMetadata
				{
					Url = avatarUrl,
					PublicId = null,
					FileSize = null,
					MimeType = mimeType
				};

				existingMedia.UpdateFile(fileMetadata, altText);
				_unitOfWork.Media.Update(existingMedia);
			}
			else
			{
				var basicInfo = new BasicMediaInfo
				{
					Url = avatarUrl,
					AltText = altText,
					MimeType = mimeType
				};

				var profileMedia = Media.CreateBasic(EntityType.User, userId, basicInfo);
				await _unitOfWork.Media.AddAsync(profileMedia);
			}

			return await _unitOfWork.SaveChangesAsync();
		}

		public async Task<BaseResponse<string>> UploadProfileAvatarAsync(Guid userId, ProfileAvtarUploadRequest request)
		{
			var existingMedia = await _unitOfWork.Media.GetPrimaryMediaAsync(EntityType.User, userId);

			// 1. Clean up old image on Cloud
			if (existingMedia != null && !string.IsNullOrEmpty(existingMedia.PublicId))
				await _supabaseService.DeleteImageAsync(existingMedia.Url, GetBucketName(EntityType.User));

			// 2. Upload new image
			using var stream = request.Avatar!.OpenReadStream();
			var url = await _supabaseService.UploadImageAsync(
				stream, request.Avatar.FileName, GetBucketName(EntityType.User));

			if (string.IsNullOrEmpty(url))
				throw AppException.Internal("Failed to upload avatar image");

			var fileMetadata = new FileMetadata
			{
				Url = url,
				PublicId = ExtractFileNameFromUrl(url),
				FileSize = request.Avatar.Length,
				MimeType = GetMimeType(request.Avatar.FileName)
			};

			if (existingMedia != null)
			{
				existingMedia.UpdateFile(fileMetadata, request.AltText);
				_unitOfWork.Media.Update(existingMedia);
			}
			else
			{
				var displayInfo = new MediaDisplayInfo
				{
					AltText = request.AltText ?? "Profile picture",
					DisplayOrder = 0,
					IsPrimary = true
				};

				var profileMedia = Media.Create(EntityType.User, userId, fileMetadata, displayInfo);
				await _unitOfWork.Media.AddAsync(profileMedia);
			}

			// 5. Commit Transaction
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to save profile avatar");

			var message = existingMedia != null
				? "Profile avatar updated successfully"
				: "Profile avatar uploaded successfully";

			return BaseResponse<string>.Ok(url, message);
		}

		public async Task<BaseResponse<string>> DeleteProfileAvatarAsync(Guid userId)
		{
			var existingMedia = await _unitOfWork.Media.GetPrimaryMediaAsync(EntityType.User, userId)
				?? throw AppException.NotFound("Profile avatar not found");

			return await DeleteMediaInternalAsync(existingMedia, userId.ToString(), "Profile avatar deleted successfully");
		}
		#endregion Profile Avatar

		#region Temporary Media
		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadReviewTemporaryMediaAsync(
		Guid? userId, ReviewUploadMediaRequest request)
		{
			var imageRequests = request.Images
				.Select((file, i) => new ImageUploadItem
				{
					File = file,
					EntityType = EntityType.Review,
					DisplayOrder = i,
					IsPrimary = false,
					AltText = null
				}).ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadProductTemporaryMediaAsync(
		Guid? userId, ProductUploadMediaRequest request)
		{
			var imageRequests = request.Images
				.Select(r => new ImageUploadItem
				{
					File = r.ImageFile,
					EntityType = EntityType.Product,
					DisplayOrder = r.DisplayOrder,
					IsPrimary = r.IsPrimary,
					AltText = r.AltText
				}).ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadVariantTemporaryMediaAsync(
			Guid? userId, VariantUploadMediaRequest request)
		{
			var imageRequests = request.Images
				.Select(r => new ImageUploadItem
				{
					File = r.ImageFile,
					EntityType = EntityType.ProductVariant,
					DisplayOrder = r.DisplayOrder,
					IsPrimary = r.IsPrimary,
					AltText = r.AltText
				}).ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadOrderReturnRequestTemporaryMediaAsync(
			Guid? userId, OrderReturnRequestUploadMediaRequest request)
		{
			var videoRequests = request.Videos
				.Select((file, i) => new ImageUploadItem
				{
					File = file,
					EntityType = EntityType.OrderReturnRequest,
					DisplayOrder = i,
					IsPrimary = false,
					AltText = null
				}).ToList();

			return await UploadTemporaryMediaBulkAsync(userId, videoRequests, ValidateVideoFile, "video");
		}
		#endregion Temporary Media

		#region Private Helpers
		private async Task<(string? Url, string? Error)> UploadToTemporaryStorageAsync(IFormFile file, EntityType entityType)
		{
			try
			{
				using var stream = file.OpenReadStream();
				var url = await _supabaseService.UploadImageAsync(
					stream, file.FileName, GetBucketName(entityType));

				return string.IsNullOrEmpty(url)
					? (null, $"Failed to upload {file.FileName}")
					: (url, null);
			}
			catch (Exception ex)
			{
				return (null, $"Failed to upload {file.FileName}: {ex.Message}");
			}
		}

		private async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadTemporaryMediaBulkAsync(
			 Guid? userId,
			 List<ImageUploadItem> items,
			 Func<IFormFile?, string?>? validator = null,
			 string mediaType = "image")
		{
			validator ??= ValidateImageFile;
			var bulkResult = new BulkActionResponse { SucceededIds = [], FailedItems = [] };
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var item in items)
			{
				var tempId = Guid.NewGuid();

				var validationError = validator(item.File);
				if (validationError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = validationError });
					continue;
				}

				var (url, uploadError) = await UploadToTemporaryStorageAsync(item.File!, item.EntityType);
				if (uploadError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = uploadError });
					continue;
				}

				var payload = new TemporaryMediaPayload
				{
					Url = url!,
					FileName = item.File!.FileName,
					FileSize = item.File.Length,
					PublicId = ExtractFileNameFromUrl(url!),
					AltText = item.AltText,
					DisplayOrder = item.DisplayOrder,
					IsPrimary = item.IsPrimary,
					UploadedByUserId = userId,
					TargetEntityType = item.EntityType,
					ExpiresIn = TimeSpan.FromHours(24)
				};

				var tempMedia = TemporaryMedia.Create(payload);

				await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
				await _unitOfWork.SaveChangesAsync();

				uploadedMedia.Add(_mapper.Map<TemporaryMediaResponse>(tempMedia));
				bulkResult.SucceededIds.Add(tempMedia.Id);
			}

			return BuildTemporaryMediaResponse(uploadedMedia, bulkResult, mediaType);
		}

		private async Task<BaseResponse<string>> DeleteMediaInternalAsync(Media media, string successData, string successMessage)
		{
			if (!string.IsNullOrEmpty(media.PublicId))
				await _supabaseService.DeleteImageAsync(media.Url, GetBucketName(media.EntityType));

			_unitOfWork.Media.Remove(media);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete media");

			return BaseResponse<string>.Ok(successData, successMessage);
		}

		private static string GetBucketName(EntityType entityType) => entityType switch
		{
			EntityType.Product => "Products",
			EntityType.ProductVariant => "ProductVariants",
			EntityType.User => "ProfileAvatars",
			EntityType.Review => "Reviews",
			EntityType.OrderReturnRequest => "OrderReturnRequests",
			_ => "Products"
		};

		private static string ExtractFileNameFromUrl(string url)
		{
			var segments = new Uri(url).AbsolutePath.Split('/');
			return segments.Length > 0 ? segments[^1] : string.Empty;
		}

		private static string? GetMimeType(string fileName) =>
			Path.GetExtension(fileName).ToLowerInvariant() switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".svg" => "image/svg+xml",
				_ => null
			};

		private static string? GetMimeTypeFromUrl(string url)
		{
			try { return GetMimeType(new Uri(url).AbsolutePath) ?? "image/jpeg"; }
			catch { return "image/jpeg"; }
		}

		private static string? ValidateImageFile(IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return "Empty or null image file";

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(extension))
				return $"Invalid image format for {file.FileName}. Allowed: jpg, jpeg, png, gif, webp";

			if (file.Length > MaxFileSize)
				return $"Image size must be less than 5MB for {file.FileName}";

			return null;
		}

		private static string? ValidateVideoFile(IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return "Empty or null video file";

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedVideoExtensions.Contains(extension))
				return $"Invalid video format for {file.FileName}. Allowed: mp4, mov, webm, m4v";

			if (file.Length > MaxVideoFileSize)
				return $"Video size must be less than 100MB for {file.FileName}";

			return null;
		}

		private static BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>> BuildTemporaryMediaResponse(
	  List<TemporaryMediaResponse> uploadedMedia, BulkActionResponse bulkResult, string mediaType)
		{
			var mediaTypePlural = $"{mediaType}s";

			if (uploadedMedia.Count == 0)
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
				  $"Failed to upload any {mediaTypePlural}",
					ResponseErrorType.BadRequest,
					[.. bulkResult.FailedItems.Select(f => f.ErrorMessage)]);

			var metadata = new BulkActionMetadata { Operations = [] };
			if (bulkResult.TotalProcessed > 0)
				metadata.Operations.Add(
					BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(
				uploadedMedia,
				metadata.Operations.Count > 0 ? metadata : null);

			var message = bulkResult.HasError
			   ? $"Uploaded {uploadedMedia.Count} {mediaType}(s). {bulkResult.FailedItems.Count} failed. Expires in 24 hours."
				: $"Uploaded {uploadedMedia.Count} temporary {mediaType}(s). Expires in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}

		#endregion Private Helpers
	}
}

