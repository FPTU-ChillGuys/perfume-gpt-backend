using MapsterMapper;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Interfaces.Repositories;
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
		private readonly IMapper _mapper;

		public MediaService(
			IMediaRepository mediaRepo,
			ISupabaseService supabaseService,
			IMapper mapper)
		{
			_mediaRepo = mediaRepo;
			_supabaseService = supabaseService;
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
	}
}
