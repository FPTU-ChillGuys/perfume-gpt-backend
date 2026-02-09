using MapsterMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Interfaces.Repositories;
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

		private readonly IMediaRepository _mediaRepo;
		private readonly ISupabaseService _supabaseService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IValidator<ProductUploadMediaRequest> _productUploadValidator;
		private readonly IValidator<VariantUploadMediaRequest> _variantUploadValidator;

		public MediaService(
			IMediaRepository mediaRepo,
			ISupabaseService supabaseService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IValidator<ProductUploadMediaRequest> productUploadValidator,
			IValidator<VariantUploadMediaRequest> variantUploadValidator)
		{
			_mediaRepo = mediaRepo;
			_supabaseService = supabaseService;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_productUploadValidator = productUploadValidator;
			_variantUploadValidator = variantUploadValidator;
		}

		#endregion Dependencies

		#region Media CRUD

		public async Task<BaseResponse<string>> DeleteMediaAsync(Guid mediaId)
		{
			var media = await _mediaRepo.GetByIdAsync(mediaId);
			if (media == null)
			{
				return BaseResponse<string>.Fail("Media not found", ResponseErrorType.NotFound);
			}

			if (media.IsDeleted)
			{
				return BaseResponse<string>.Fail("Media already deleted", ResponseErrorType.BadRequest);
			}

			if (media.IsPrimary)
			{
				return BaseResponse<string>.Fail("Cannot delete primary media. Please set another media as primary before deleting.", ResponseErrorType.BadRequest);
			}

			return await DeleteMediaInternalAsync(media, mediaId.ToString(), "Media deleted successfully");
		}

		public async Task<BaseResponse<string>> SetPrimaryMediaAsync(Guid mediaId)
		{
			var media = await _mediaRepo.GetByIdAsync(mediaId);
			if (media == null || media.IsDeleted)
			{
				return BaseResponse<string>.Fail("Media not found", ResponseErrorType.NotFound);
			}

			var existingPrimary = await _mediaRepo.GetPrimaryMediaAsync(media.EntityType, media.EntityId);
			if (existingPrimary != null && existingPrimary.Id != mediaId)
			{
				existingPrimary.IsPrimary = false;
				_mediaRepo.Update(existingPrimary);
			}

			media.IsPrimary = true;
			_mediaRepo.Update(media);

			var saved = await _mediaRepo.SaveChangesAsync();
			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to set primary media", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(mediaId.ToString(), "Primary media set successfully");
		}

		public async Task<BaseResponse<List<MediaResponse>>> GetMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaList = await _mediaRepo.GetMediaByEntityTypeAsync(entityType, entityId);
			var response = _mapper.Map<List<MediaResponse>>(mediaList);
			return BaseResponse<List<MediaResponse>>.Ok(response, "Media retrieved successfully");
		}

		public async Task<BaseResponse<MediaResponse?>> GetPrimaryMediaAsync(EntityType entityType, Guid entityId)
		{
			var media = await _mediaRepo.GetPrimaryMediaAsync(entityType, entityId);
			var response = media != null ? _mapper.Map<MediaResponse>(media) : null;
			return BaseResponse<MediaResponse?>.Ok(response, media != null ? "Primary media retrieved successfully" : "No primary media found");
		}

		public async Task<BaseResponse<string>> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaList = await _mediaRepo.GetMediaByEntityTypeAsync(entityType, entityId);

			var bucketName = GetBucketName(entityType);
			foreach (var media in mediaList)
			{
				await _supabaseService.DeleteImageAsync(media.Url, bucketName);
			}

			var count = await _mediaRepo.DeleteAllMediaByEntityAsync(entityType, entityId);
			var saved = await _mediaRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete media", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(count.ToString(), $"{count} media items deleted successfully");
		}

		#endregion Media CRUD

		#region Profile Avatar

		public async Task<bool> CreateProfileAvatarFromUrlAsync(Guid userId, string avatarUrl, string? altText = null)
		{
			if (string.IsNullOrWhiteSpace(avatarUrl))
			{
				return false;
			}

			try
			{
				var existingMedia = await _mediaRepo.GetPrimaryMediaAsync(EntityType.User, userId);
				if (existingMedia != null)
				{
					existingMedia.Url = avatarUrl.Trim();
					existingMedia.AltText = altText ?? existingMedia.AltText;
					_mediaRepo.Update(existingMedia);
					return await _mediaRepo.SaveChangesAsync();
				}

				var profileMedia = new Media
				{
					Url = avatarUrl.Trim(),
					AltText = altText ?? "Profile picture",
					EntityType = EntityType.User,
					UserId = userId,
					DisplayOrder = 0,
					IsPrimary = true,
					MimeType = GetMimeTypeFromUrl(avatarUrl)
				};

				await _mediaRepo.AddAsync(profileMedia);
				return await _mediaRepo.SaveChangesAsync();
			}
			catch
			{
				return false;
			}
		}

		public async Task<BaseResponse<string>> UploadProfileAvatarAsync(Guid userId, UploadProfileAvatarRequest request)
		{
			if (request.Avatar == null || request.Avatar.Length == 0)
			{
				return BaseResponse<string>.Fail("Avatar image is required", ResponseErrorType.BadRequest);
			}

			// Validate file type
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(request.Avatar.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return BaseResponse<string>.Fail(
					"Invalid image format. Allowed: jpg, jpeg, png, gif, webp",
					ResponseErrorType.BadRequest);
			}

			// Validate file size (max 5MB)
			const long maxFileSize = 5 * 1024 * 1024;
			if (request.Avatar.Length > maxFileSize)
			{
				return BaseResponse<string>.Fail(
					"Avatar image size must be less than 5MB",
					ResponseErrorType.BadRequest);
			}

			try
			{
				var existingMedia = await _mediaRepo.GetPrimaryMediaAsync(EntityType.User, userId);

				if (existingMedia != null && !string.IsNullOrEmpty(existingMedia.PublicId))
				{
					await _supabaseService.DeleteImageAsync(existingMedia.Url, GetBucketName(EntityType.User));
				}

				using var stream = request.Avatar.OpenReadStream();
				var url = await _supabaseService.UploadImageAsync(stream, request.Avatar.FileName, GetBucketName(EntityType.User));

				if (string.IsNullOrEmpty(url))
				{
					return BaseResponse<string>.Fail("Failed to upload avatar image", ResponseErrorType.InternalError);
				}

				if (existingMedia != null)
				{
					existingMedia.Url = url;
					existingMedia.AltText = request.AltText ?? existingMedia.AltText;
					existingMedia.PublicId = ExtractFileNameFromUrl(url);
					existingMedia.FileSize = request.Avatar.Length;
					existingMedia.MimeType = GetMimeType(request.Avatar.FileName);
					_mediaRepo.Update(existingMedia);
					await _mediaRepo.SaveChangesAsync();

					return BaseResponse<string>.Ok(url, "Profile avatar updated successfully");
				}

				var profileMedia = new Media
				{
					Url = url,
					AltText = request.AltText ?? "Profile picture",
					EntityType = EntityType.User,
					UserId = userId,
					DisplayOrder = 0,
					IsPrimary = true,
					PublicId = ExtractFileNameFromUrl(url),
					FileSize = request.Avatar.Length,
					MimeType = GetMimeType(request.Avatar.FileName)
				};

				await _mediaRepo.AddAsync(profileMedia);
				var saved = await _mediaRepo.SaveChangesAsync();

				if (!saved)
				{
					return BaseResponse<string>.Fail("Failed to save profile avatar", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(url, "Profile avatar uploaded successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Failed to upload profile avatar: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> DeleteProfileAvatarAsync(Guid userId)
		{
			var existingMedia = await _mediaRepo.GetPrimaryMediaAsync(EntityType.User, userId);
			if (existingMedia == null)
			{
				return BaseResponse<string>.Fail("Profile avatar not found", ResponseErrorType.NotFound);
			}

			return await DeleteMediaInternalAsync(existingMedia, userId.ToString(), "Profile avatar deleted successfully");
		}

		#endregion Profile Avatar

		#region Private Helpers

		private async Task<BaseResponse<string>> DeleteMediaInternalAsync(Media media, string successData, string successMessage)
		{
			try
			{
				if (!string.IsNullOrEmpty(media.PublicId))
				{
					await _supabaseService.DeleteImageAsync(media.Url, GetBucketName(media.EntityType));
				}

				_mediaRepo.Remove(media);
				var saved = await _mediaRepo.SaveChangesAsync();

				if (!saved)
				{
					return BaseResponse<string>.Fail("Failed to delete media", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(successData, successMessage);
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Failed to delete media: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		private static string GetBucketName(EntityType entityType)
		{
			return entityType switch
			{
				EntityType.Product => "Products",
				EntityType.ProductVariant => "ProductVariants",
				EntityType.User => "ProfileAvatars",
				EntityType.Review => "Reviews",
				_ => "Products"
			};
		}

		private static string ExtractFileNameFromUrl(string url)
		{
			try
			{
				var uri = new Uri(url);
				var segments = uri.AbsolutePath.Split('/');
				return segments.Length > 0 ? segments[^1] : string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string? GetMimeType(string fileName)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			return extension switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".svg" => "image/svg+xml",
				_ => null
			};
		}

		private static string? GetMimeTypeFromUrl(string url)
		{
			try
			{
				var uri = new Uri(url);
				var path = uri.AbsolutePath;
				return GetMimeType(path) ?? "image/jpeg";
			}
			catch
			{
				return "image/jpeg";
			}
		}

		private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
		private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

		private static string? ValidateImageFile(IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return "Empty or null image file";

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(extension))
				return $"Invalid image format for {file.FileName}. Allowed: jpg, jpeg, png, gif, webp";

			if (file.Length > MaxFileSize)
				return $"Image size must be less than 5MB for {file.FileName}";

			return null; // Valid
		}

		private async Task<(string? Url, string? Error)> UploadToTemporaryStorageAsync(IFormFile file)
		{
			try
			{
				using var stream = file.OpenReadStream();
				var url = await _supabaseService.UploadPreviewImageAsync(stream, file.FileName);

				if (string.IsNullOrEmpty(url))
					return (null, $"Failed to upload {file.FileName}");

				return (url, null);
			}
			catch (Exception ex)
			{
				return (null, $"Failed to upload {file.FileName}: {ex.Message}");
			}
		}

		private static TemporaryMedia CreateTemporaryMediaRecord(
			string url,
			IFormFile file,
			Guid? userId,
			EntityType targetEntityType,
			int displayOrder,
			bool isPrimary = false,
			string? altText = null)
		{
			return new TemporaryMedia
			{
				Url = url,
				AltText = altText,
				DisplayOrder = displayOrder,
				IsPrimary = isPrimary,
				PublicId = ExtractFileNameFromUrl(url),
				FileSize = file.Length,
				MimeType = GetMimeType(file.FileName),
				UploadedByUserId = userId,
				TargetEntityType = targetEntityType,
				ExpiresAt = DateTime.UtcNow.AddHours(24),
			};
		}

		private static BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>> BuildTemporaryMediaResponse(
			List<TemporaryMediaResponse> uploadedMedia,
			BulkActionResponse bulkResult)
		{
			if (uploadedMedia.Count == 0)
			{
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
					"Failed to upload any images",
					ResponseErrorType.BadRequest,
					[.. bulkResult.FailedItems.Select(f => f.ErrorMessage)]);
			}

			var metadata = new BulkActionMetadata();
			if (bulkResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));
			}

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(
				uploadedMedia,
				metadata.Operations.Count > 0 ? metadata : null);

			var message = bulkResult.HasError
				? $"Successfully uploaded {uploadedMedia.Count} temporary image(s). {bulkResult.FailedItems.Count} failed. They will expire in 24 hours."
				: $"Successfully uploaded {uploadedMedia.Count} temporary image(s). They will expire in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}

		#endregion Private Helpers

		#region Temporary Media

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadReviewTemporaryMediaAsync(Guid? userId, ReviewUploadMediaRequest request)
		{
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			for (int i = 0; i < request.Images.Count; i++)
			{
				var imageFile = request.Images[i];
				var tempId = Guid.NewGuid();

				var validationError = ValidateImageFile(imageFile);
				if (validationError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = validationError });
					continue;
				}

				var (url, uploadError) = await UploadToTemporaryStorageAsync(imageFile!);
				if (uploadError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = uploadError });
					continue;
				}

				var tempMedia = CreateTemporaryMediaRecord(
					url!,
					imageFile!,
					userId,
					EntityType.Review,
					displayOrder: i);

				await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
				await _unitOfWork.SaveChangesAsync();

				uploadedMedia.Add(_mapper.Map<TemporaryMediaResponse>(tempMedia));
				bulkResult.SucceededIds.Add(tempMedia.Id);
			}

			return BuildTemporaryMediaResponse(uploadedMedia, bulkResult);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadProductTemporaryMediaAsync(Guid? userId, ProductUploadMediaRequest request)
		{
			if (_productUploadValidator != null)
			{
				var validationResult = await _productUploadValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
						"Validation failed",
						ResponseErrorType.BadRequest,
						[.. validationResult.Errors.Select(e => e.ErrorMessage)]
					);
				}
			}
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var imageRequest in request.Images)
			{
				var tempId = Guid.NewGuid();

				var validationError = ValidateImageFile(imageRequest.ImageFile);
				if (validationError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = validationError });
					continue;
				}

				var (url, uploadError) = await UploadToTemporaryStorageAsync(imageRequest.ImageFile!);
				if (uploadError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = uploadError });
					continue;
				}

				var tempMedia = CreateTemporaryMediaRecord(
					url!,
					imageRequest.ImageFile!,
					userId,
					EntityType.Product,
					displayOrder: imageRequest.DisplayOrder,
					isPrimary: imageRequest.IsPrimary,
					altText: imageRequest.AltText);

				await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
				await _unitOfWork.SaveChangesAsync();

				uploadedMedia.Add(_mapper.Map<TemporaryMediaResponse>(tempMedia));
				bulkResult.SucceededIds.Add(tempMedia.Id);
			}

			return BuildTemporaryMediaResponse(uploadedMedia, bulkResult);
		}

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadVariantTemporaryMediaAsync(Guid? userId, VariantUploadMediaRequest request)
		{
			if (_productUploadValidator != null)
			{
				var validationResult = await _variantUploadValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
						"Validation failed",
						ResponseErrorType.BadRequest,
						[.. validationResult.Errors.Select(e => e.ErrorMessage)]
					);
				}
			}
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var imageRequest in request.Images)
			{
				var tempId = Guid.NewGuid();

				var validationError = ValidateImageFile(imageRequest.ImageFile);
				if (validationError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = validationError });
					continue;
				}

				var (url, uploadError) = await UploadToTemporaryStorageAsync(imageRequest.ImageFile!);
				if (uploadError != null)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = uploadError });
					continue;
				}

				var tempMedia = CreateTemporaryMediaRecord(
					url!,
					imageRequest.ImageFile!,
					userId,
					EntityType.ProductVariant,
					displayOrder: imageRequest.DisplayOrder,
					isPrimary: imageRequest.IsPrimary,
					altText: imageRequest.AltText);

				await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
				await _unitOfWork.SaveChangesAsync();

				uploadedMedia.Add(_mapper.Map<TemporaryMediaResponse>(tempMedia));
				bulkResult.SucceededIds.Add(tempMedia.Id);
			}

			return BuildTemporaryMediaResponse(uploadedMedia, bulkResult);
		}

		#endregion Temporary Media
	}
}

