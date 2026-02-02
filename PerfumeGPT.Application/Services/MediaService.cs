using MapsterMapper;
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
		private readonly IMediaRepository _mediaRepo;
		private readonly ISupabaseService _supabaseService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public MediaService(
			IMediaRepository mediaRepo,
			ISupabaseService supabaseService,
			IUnitOfWork unitOfWork,
			IMapper mapper)
		{
			_mediaRepo = mediaRepo;
			_supabaseService = supabaseService;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<MediaResponse>> UploadMediaAsync(
			Stream fileStream,
			string fileName,
			EntityType entityType,
			Guid entityId,
			string? altText = null,
			int displayOrder = 0,
			bool isPrimary = false)
		{
			try
			{
				// Determine bucket based on entity type
				var bucketName = GetBucketName(entityType);

				// Upload to Supabase
				var url = await _supabaseService.UploadImageAsync(fileStream, fileName, bucketName);
				if (string.IsNullOrEmpty(url))
				{
					return BaseResponse<MediaResponse>.Fail("Failed to upload image", ResponseErrorType.InternalError);
				}

				// Extract file name from URL as PublicId
				var publicId = ExtractFileNameFromUrl(url);

				// Get file info
				long fileSize = 0;
				if (fileStream.CanSeek)
				{
					fileSize = fileStream.Length;
				}

				var mimeType = GetMimeType(fileName);

				// If this is set as primary, unset other primary images for this entity
				if (isPrimary)
				{
					var existingPrimary = await _mediaRepo.GetPrimaryMediaAsync(entityType, entityId);
					if (existingPrimary != null)
					{
						existingPrimary.IsPrimary = false;
						_mediaRepo.Update(existingPrimary);
					}
				}

				// Create media entity
				var media = new Media
				{
					Url = url,
					AltText = altText,
					EntityType = entityType,
					ProductId = entityType == EntityType.Product ? entityId : null,
					ProductVariantId = entityType == EntityType.ProductVariant ? entityId : null,
					DisplayOrder = displayOrder,
					IsPrimary = isPrimary,
					PublicId = publicId,
					FileSize = fileSize > 0 ? fileSize : null,
					MimeType = mimeType
				};

				await _mediaRepo.AddAsync(media);
				var saved = await _mediaRepo.SaveChangesAsync();

				if (!saved)
				{
					// Cleanup uploaded file
					await _supabaseService.DeleteImageAsync(url, bucketName);
					return BaseResponse<MediaResponse>.Fail("Failed to save media record", ResponseErrorType.InternalError);
				}

				var response = _mapper.Map<MediaResponse>(media);
				return BaseResponse<MediaResponse>.Ok(response, "Media uploaded successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<MediaResponse>.Fail($"Error uploading media: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

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

			// Delete from Supabase
			var bucketName = GetBucketName(media.EntityType);
			await _supabaseService.DeleteImageAsync(media.Url, bucketName);

			// Soft delete in database even if Supabase delete fails
			_mediaRepo.Remove(media);
			var saved = await _mediaRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete media", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(mediaId.ToString(), "Media deleted successfully");
		}

		public async Task<BaseResponse<string>> SetPrimaryMediaAsync(Guid mediaId)
		{
			var media = await _mediaRepo.GetByIdAsync(mediaId);
			if (media == null || media.IsDeleted)
			{
				return BaseResponse<string>.Fail("Media not found", ResponseErrorType.NotFound);
			}

			// Unset other primary images for this entity
			var existingPrimary = await _mediaRepo.GetPrimaryMediaAsync(media.EntityType, media.EntityId);
			if (existingPrimary != null && existingPrimary.Id != mediaId)
			{
				existingPrimary.IsPrimary = false;
				_mediaRepo.Update(existingPrimary);
			}

			// Set this as primary
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
			var mediaList = await _mediaRepo.GetMediaByEntityAsync(entityType, entityId);
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
			var mediaList = await _mediaRepo.GetMediaByEntityAsync(entityType, entityId);

			// Delete from Supabase
			var bucketName = GetBucketName(entityType);
			foreach (var media in mediaList)
			{
				await _supabaseService.DeleteImageAsync(media.Url, bucketName);
			}

			// Soft delete in database
			var count = await _mediaRepo.DeleteAllMediaByEntityAsync(entityType, entityId);
			var saved = await _mediaRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete media", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(count.ToString(), $"{count} media items deleted successfully");
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

		// ==================== TEMPORARY MEDIA METHODS ====================

		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadTemporaryMediaAsync(Guid? userId, ReviewUploadMediaRequest request, EntityType targetEntityType = EntityType.Review)
		{
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			// Auto-assign display order based on list index
			for (int i = 0; i < request.Images.Count; i++)
			{
				var imageFile = request.Images[i];
				var tempId = Guid.NewGuid(); // Temporary ID for tracking before upload

				if (imageFile == null || imageFile.Length == 0)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = "Empty or null image file"
					});
					continue;
				}

				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Invalid image format for {imageFile.FileName}. Allowed: jpg, jpeg, png, gif, webp"
					});
					continue;
				}

				// Validate file size (max 5MB)
				const long maxFileSize = 5 * 1024 * 1024;
				if (imageFile.Length > maxFileSize)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Image size must be less than 5MB for {imageFile.FileName}"
					});
					continue;
				}

				try
				{
					// Upload to temporary bucket (different buckets based on entity type)
					using var stream = imageFile.OpenReadStream();
					var url = await _supabaseService.UploadPreviewImageAsync(stream, imageFile.FileName);

					if (string.IsNullOrEmpty(url))
					{
						bulkResult.FailedItems.Add(new BulkActionError
						{
							Id = tempId,
							ErrorMessage = $"Failed to upload {imageFile.FileName}"
						});
						continue;
					}

					// Create temporary media record
					var tempMedia = new TemporaryMedia
					{
						Url = url,
						AltText = null,
						DisplayOrder = i,
						PublicId = ExtractFileNameFromUrl(url),
						FileSize = imageFile.Length,
						MimeType = GetMimeType(imageFile.FileName),
						UploadedByUserId = userId,
						TargetEntityType = targetEntityType, // Track target entity
						ExpiresAt = DateTime.UtcNow.AddHours(24),
					};

					await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
					await _unitOfWork.SaveChangesAsync();

					var response = _mapper.Map<TemporaryMediaResponse>(tempMedia);
					uploadedMedia.Add(response);
					bulkResult.SucceededIds.Add(tempMedia.Id);
				}
				catch (Exception ex)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Failed to upload {imageFile.FileName}: {ex.Message}"
					});
				}
			}

			if (uploadedMedia.Count == 0)
			{
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
					"Failed to upload any images",
					ResponseErrorType.BadRequest,
					bulkResult.FailedItems.Select(f => f.ErrorMessage).ToList()
				);
			}

			// Build metadata if there are any operations
			var metadata = new BulkActionMetadata();
			if (bulkResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));
			}

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(uploadedMedia, metadata.Operations.Count > 0 ? metadata : null);
			var message = bulkResult.HasError
				? $"Successfully uploaded {uploadedMedia.Count} temporary image(s). {bulkResult.FailedItems.Count} failed. They will expire in 24 hours."
				: $"Successfully uploaded {uploadedMedia.Count} temporary image(s). They will expire in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteTemporaryMediaAsync(Guid temporaryMediaId)
		{
			var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(temporaryMediaId);
			if (tempMedia == null)
			{
				return BaseResponse<string>.Fail("Temporary media not found", ResponseErrorType.NotFound);
			}

			if (!string.IsNullOrEmpty(tempMedia.PublicId))
			{
				await _supabaseService.DeletePreviewImageAsync(tempMedia.PublicId);
			}

			_unitOfWork.TemporaryMedia.Remove(tempMedia);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok("Temporary media deleted successfully");
		}

		public async Task<BaseResponse<List<TemporaryMediaResponse>>> GetUserTemporaryMediaAsync(Guid userId)
		{
			var tempMedia = await _unitOfWork.TemporaryMedia.GetByUserIdAsync(userId);
			var response = _mapper.Map<List<TemporaryMediaResponse>>(tempMedia);
			return BaseResponse<List<TemporaryMediaResponse>>.Ok(response);
		}

		/// <summary>
		/// Upload temporary media for Products (with IsPrimary and DisplayOrder)
		/// </summary>
		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadTemporaryProductMediaAsync(Guid? userId, ProductUploadMediaRequest request)
		{
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var imageRequest in request.Images)
			{
				var tempId = Guid.NewGuid(); // Temporary ID for tracking before upload

				if (imageRequest.ImageFile == null || imageRequest.ImageFile.Length == 0)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = "Empty or null image file"
					});
					continue;
				}

				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageRequest.ImageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Invalid image format for {imageRequest.ImageFile.FileName}. Allowed: jpg, jpeg, png, gif, webp"
					});
					continue;
				}

				// Validate file size (max 5MB)
				const long maxFileSize = 5 * 1024 * 1024;
				if (imageRequest.ImageFile.Length > maxFileSize)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Image size must be less than 5MB for {imageRequest.ImageFile.FileName}"
					});
					continue;
				}

				try
				{
					// Upload to temporary bucket
					using var stream = imageRequest.ImageFile.OpenReadStream();
					var url = await _supabaseService.UploadPreviewImageAsync(stream, imageRequest.ImageFile.FileName);

					if (string.IsNullOrEmpty(url))
					{
						bulkResult.FailedItems.Add(new BulkActionError
						{
							Id = tempId,
							ErrorMessage = $"Failed to upload {imageRequest.ImageFile.FileName}"
						});
						continue;
					}

					// Create temporary media record WITH metadata
					var tempMedia = new TemporaryMedia
					{
						Url = url,
						AltText = imageRequest.AltText,
						DisplayOrder = imageRequest.DisplayOrder, // User-specified order
						IsPrimary = imageRequest.IsPrimary, // Store IsPrimary flag
						PublicId = ExtractFileNameFromUrl(url),
						FileSize = imageRequest.ImageFile.Length,
						MimeType = GetMimeType(imageRequest.ImageFile.FileName),
						UploadedByUserId = userId,
						TargetEntityType = EntityType.Product,
						ExpiresAt = DateTime.UtcNow.AddHours(24),
					};

					await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
					await _unitOfWork.SaveChangesAsync();

					var response = _mapper.Map<TemporaryMediaResponse>(tempMedia);
					uploadedMedia.Add(response);
					bulkResult.SucceededIds.Add(tempMedia.Id);
				}
				catch (Exception ex)
				{
					bulkResult.FailedItems.Add(new BulkActionError
					{
						Id = tempId,
						ErrorMessage = $"Failed to upload {imageRequest.ImageFile.FileName}: {ex.Message}"
					});
				}
			}

			if (uploadedMedia.Count == 0)
			{
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail(
					"Failed to upload any images",
					ResponseErrorType.BadRequest,
					bulkResult.FailedItems.Select(f => f.ErrorMessage).ToList()
				);
			}

			// Build metadata if there are any operations
			var metadata = new BulkActionMetadata();
			if (bulkResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));
			}

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(uploadedMedia, metadata.Operations.Count > 0 ? metadata : null);
			var message = bulkResult.HasError
				? $"Successfully uploaded {uploadedMedia.Count} temporary image(s). {bulkResult.FailedItems.Count} failed. They will expire in 24 hours."
				: $"Successfully uploaded {uploadedMedia.Count} temporary image(s). They will expire in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}

		/// <summary>
		/// Upload temporary media for Variant (single image only)
		/// </summary>
		public async Task<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>> UploadTemporaryVariantMediaAsync(Guid? userId, VariantUploadMediaRequest request)
		{
			var bulkResult = new BulkActionResponse();
			var uploadedMedia = new List<TemporaryMediaResponse>();

			foreach (var imageRequest in request.Images)
			{
				var tempId = Guid.NewGuid();

				if (imageRequest.ImageFile == null || imageRequest.ImageFile.Length == 0)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = "Empty or null image file" });
					continue;
				}

				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageRequest.ImageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = $"Invalid image format for {imageRequest.ImageFile.FileName}. Allowed: jpg, jpeg, png, gif, webp" });
					continue;
				}

				const long maxFileSize = 5 * 1024 * 1024;
				if (imageRequest.ImageFile.Length > maxFileSize)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = $"Image size must be less than 5MB for {imageRequest.ImageFile.FileName}" });
					continue;
				}

				try
				{
					using var stream = imageRequest.ImageFile.OpenReadStream();
					var url = await _supabaseService.UploadPreviewImageAsync(stream, imageRequest.ImageFile.FileName);

					if (string.IsNullOrEmpty(url))
					{
						bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = $"Failed to upload {imageRequest.ImageFile.FileName}" });
						continue;
					}

					var tempMedia = new TemporaryMedia
					{
						Url = url,
						AltText = imageRequest.AltText,
						DisplayOrder = imageRequest.DisplayOrder,
						IsPrimary = imageRequest.IsPrimary,
						PublicId = ExtractFileNameFromUrl(url),
						FileSize = imageRequest.ImageFile.Length,
						MimeType = GetMimeType(imageRequest.ImageFile.FileName),
						UploadedByUserId = userId,
						TargetEntityType = EntityType.ProductVariant,
						ExpiresAt = DateTime.UtcNow.AddHours(24),
					};

					await _unitOfWork.TemporaryMedia.AddAsync(tempMedia);
					await _unitOfWork.SaveChangesAsync();

					var response = _mapper.Map<TemporaryMediaResponse>(tempMedia);
					uploadedMedia.Add(response);
					bulkResult.SucceededIds.Add(tempMedia.Id);
				}
				catch (Exception ex)
				{
					bulkResult.FailedItems.Add(new BulkActionError { Id = tempId, ErrorMessage = $"Failed to upload {imageRequest.ImageFile.FileName}: {ex.Message}" });
				}
			}

			if (uploadedMedia.Count == 0)
			{
				return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Fail("Failed to upload any images", ResponseErrorType.BadRequest, bulkResult.FailedItems.Select(f => f.ErrorMessage).ToList());
			}

			var metadata = new BulkActionMetadata();
			if (bulkResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Temporary Media Upload", bulkResult));
			}

			var result = new BulkActionResult<List<TemporaryMediaResponse>>(uploadedMedia, metadata.Operations.Count > 0 ? metadata : null);
			var message = bulkResult.HasError
				? $"Successfully uploaded {uploadedMedia.Count} temporary image(s). {bulkResult.FailedItems.Count} failed. They will expire in 24 hours."
				: $"Successfully uploaded {uploadedMedia.Count} temporary image(s). They will expire in 24 hours.";

			return BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>.Ok(result, message);
		}
	}
}

