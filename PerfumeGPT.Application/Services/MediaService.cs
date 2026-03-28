using FluentValidation;
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

namespace PerfumeGPT.Application.Services
{
	public class MediaService : IMediaService
	{
		#region Dependencies
		private readonly ISupabaseService _supabaseService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IValidator<ProductUploadMediaRequest> _productUploadValidator;
		private readonly IValidator<VariantUploadMediaRequest> _variantUploadValidator;
		private readonly IValidator<ProfileAvtarUploadRequest> _profileAvtarUploadValidator;

		private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
		private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

		public MediaService(
			ISupabaseService supabaseService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IValidator<ProductUploadMediaRequest> productUploadValidator,
			IValidator<VariantUploadMediaRequest> variantUploadValidator,
			IValidator<ProfileAvtarUploadRequest> profileAvtarUploadValidator)
		{
			_supabaseService = supabaseService;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_productUploadValidator = productUploadValidator;
			_variantUploadValidator = variantUploadValidator;
			_profileAvtarUploadValidator = profileAvtarUploadValidator;
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

			var saved = await _unitOfWork.Media.SaveChangesAsync();
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
			var saved = await _unitOfWork.Media.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete media");

			return BaseResponse<string>.Ok(count.ToString(), $"{count} media items deleted successfully");
		}
		#endregion Media CRUD

		#region Profile Avatar
		public async Task<bool> CreateProfileAvatarFromUrlAsync(Guid userId, string avatarUrl, string? altText = null)
		{
			if (string.IsNullOrWhiteSpace(avatarUrl)) return false;

			var existingMedia = await _unitOfWork.Media.GetPrimaryMediaAsync(EntityType.User, userId);
			if (existingMedia != null)
			{
				existingMedia.UpdateUrl(avatarUrl.Trim(), null, null, GetMimeTypeFromUrl(avatarUrl), altText);
				_unitOfWork.Media.Update(existingMedia);
				return await _unitOfWork.Media.SaveChangesAsync();
			}

			var profileMedia = Media.CreateFromUrl(
				EntityType.User, userId, avatarUrl, altText, GetMimeTypeFromUrl(avatarUrl));

			await _unitOfWork.Media.AddAsync(profileMedia);
			return await _unitOfWork.Media.SaveChangesAsync();
		}

		public async Task<BaseResponse<string>> UploadProfileAvatarAsync(Guid userId, ProfileAvtarUploadRequest request)
		{
			var validationResult = await _profileAvtarUploadValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var existingMedia = await _unitOfWork.Media.GetPrimaryMediaAsync(EntityType.User, userId);

			if (existingMedia != null && !string.IsNullOrEmpty(existingMedia.PublicId))
				await _supabaseService.DeleteImageAsync(existingMedia.Url, GetBucketName(EntityType.User));

			using var stream = request.Avatar!.OpenReadStream();
			var url = await _supabaseService.UploadImageAsync(
				stream, request.Avatar.FileName, GetBucketName(EntityType.User));

			if (string.IsNullOrEmpty(url))
				throw AppException.Internal("Failed to upload avatar image");

			if (existingMedia != null)
			{
				existingMedia.UpdateUrl(
					url,
					ExtractFileNameFromUrl(url),
					request.Avatar.Length,
					GetMimeType(request.Avatar.FileName),
					request.AltText);
				_unitOfWork.Media.Update(existingMedia);
				await _unitOfWork.Media.SaveChangesAsync();
				return BaseResponse<string>.Ok(url, "Profile avatar updated successfully");
			}

			var profileMedia = Media.CreateForEntity(
				EntityType.User, userId, url,
				request.AltText ?? "Profile picture",
				displayOrder: 0, isPrimary: true,
				ExtractFileNameFromUrl(url),
				request.Avatar.Length,
				GetMimeType(request.Avatar.FileName));

			await _unitOfWork.Media.AddAsync(profileMedia);
			var saved = await _unitOfWork.Media.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to save profile avatar");

			return BaseResponse<string>.Ok(url, "Profile avatar uploaded successfully");
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
				.Select((file, i) => new ImageUploadItem(file, EntityType.Review, i, false, null))
				.ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadProductTemporaryMediaAsync(
		Guid? userId, ProductUploadMediaRequest request)
		{
			var validationResult = await _productUploadValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var imageRequests = request.Images
				.Select(r => new ImageUploadItem(r.ImageFile, EntityType.Product, r.DisplayOrder, r.IsPrimary, r.AltText))
				.ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadVariantTemporaryMediaAsync(
			Guid? userId, VariantUploadMediaRequest request)
		{
			var validationResult = await _variantUploadValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var imageRequests = request.Images
				.Select(r => new ImageUploadItem(r.ImageFile, EntityType.ProductVariant, r.DisplayOrder, r.IsPrimary, r.AltText))
				.ToList();

			return await UploadTemporaryMediaBulkAsync(userId, imageRequests);
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

		private async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadTemporaryMediaBulkAsync(Guid? userId, List<ImageUploadItem> items)
		{
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var item in items)
			{
				var tempId = Guid.NewGuid();

				var validationError = ValidateImageFile(item.File);
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

				var tempMedia = TemporaryMedia.Create(
					  url!,
					  item.File!.FileName,
					  item.File.Length,
					  userId,
					  item.EntityType,
					  item.DisplayOrder,
					  item.IsPrimary,
					  item.AltText,
					  ExtractFileNameFromUrl(url!),
					  TimeSpan.FromHours(24));

				await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
				await _unitOfWork.SaveChangesAsync();

				uploadedMedia.Add(_mapper.Map<TemporaryMediaResponse>(tempMedia));
				bulkResult.SucceededIds.Add(tempMedia.Id);
			}

			return BuildTemporaryMediaResponse(uploadedMedia, bulkResult);
		}

		private async Task<BaseResponse<string>> DeleteMediaInternalAsync(Media media, string successData, string successMessage)
		{
			if (!string.IsNullOrEmpty(media.PublicId))
				await _supabaseService.DeleteImageAsync(media.Url, GetBucketName(media.EntityType));

			_unitOfWork.Media.Remove(media);
			var saved = await _unitOfWork.Media.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete media");

			return BaseResponse<string>.Ok(successData, successMessage);
		}

		private static string GetBucketName(EntityType entityType) => entityType switch
		{
			EntityType.Product => "Products",
			EntityType.ProductVariant => "ProductVariants",
			EntityType.User => "ProfileAvatars",
			EntityType.Review => "Reviews",
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

		private static BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>> BuildTemporaryMediaResponse(
		List<TemporaryMediaResponse> uploadedMedia, BulkActionResponse bulkResult)
		{
			if (uploadedMedia.Count == 0)
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
					"Failed to upload any images",
					ResponseErrorType.BadRequest,
					[.. bulkResult.FailedItems.Select(f => f.ErrorMessage)]);

			var metadata = new BulkActionMetadata();
			if (bulkResult.TotalProcessed > 0)
				metadata.Operations.Add(
					BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(
				uploadedMedia,
				metadata.Operations.Count > 0 ? metadata : null);

			var message = bulkResult.HasError
				? $"Uploaded {uploadedMedia.Count} image(s). {bulkResult.FailedItems.Count} failed. Expires in 24 hours."
				: $"Uploaded {uploadedMedia.Count} temporary image(s). Expires in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}

		#endregion Private Helpers
	}
}

