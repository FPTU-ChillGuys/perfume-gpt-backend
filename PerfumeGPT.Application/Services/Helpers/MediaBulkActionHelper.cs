using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers
{
	public class MediaBulkActionHelper
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISupabaseService _supabaseService;

		private static readonly HashSet<EntityType> SupportedEntityTypes =
		[
			EntityType.Review,
			EntityType.Product,
		   EntityType.ProductVariant,
			EntityType.OrderReturnRequest
		];

		public MediaBulkActionHelper(IUnitOfWork unitOfWork, ISupabaseService supabaseService)
		{
			_unitOfWork = unitOfWork;
			_supabaseService = supabaseService;
		}

		public async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
		List<Guid> temporaryMediaIds,
		EntityType entityType,
		Guid entityId)
		{
			if (!SupportedEntityTypes.Contains(entityType))
				throw AppException.BadRequest($"Unsupported entity type: {entityType}");

			var response = new BulkActionResponse();

			foreach (var tempMediaId in temporaryMediaIds)
			{
				var (success, errorMessage) = await ConvertSingleAsync(tempMediaId, entityType, entityId);

				if (success)
					response.SucceededIds.Add(tempMediaId);
				else
					response.FailedItems.Add(new BulkActionError { Id = tempMediaId, ErrorMessage = errorMessage! });
			}

			if (response.SucceededIds.Count > 0)
				await _unitOfWork.SaveChangesAsync();

			return response;
		}

		public async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			var response = new BulkActionResponse();

			foreach (var mediaId in mediaIds)
			{
				var media = await _unitOfWork.Media.GetByIdAsync(mediaId);
				if (media == null || media.IsDeleted)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = mediaId,
						ErrorMessage = "Media not found."
					});
					continue;
				}

				try
				{
					media.EnsureNotPrimary();
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError { Id = mediaId, ErrorMessage = ex.Message });
					continue;
				}

				if (!string.IsNullOrEmpty(media.PublicId))
					await _supabaseService.DeleteImageAsync(media.Url, GetBucketName(media.EntityType));

				_unitOfWork.Media.Remove(media);
				response.SucceededIds.Add(mediaId);
			}

			if (response.SucceededIds.Count > 0)
				await _unitOfWork.SaveChangesAsync();

			return response;
		}

		#region Private Helpers
		private async Task<(bool Success, string? Error)> ConvertSingleAsync(
		Guid tempMediaId,
		EntityType entityType,
		Guid entityId)
		{
			var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
			if (tempMedia == null)
				return (false, "Temporary media not found.");

			try
			{
				tempMedia.EnsureNotExpired();
			}
			catch (Exception ex)
			{
				return (false, ex.Message);
			}

			var media = Media.CreateForEntity(
				entityType, entityId,
				tempMedia.Url, tempMedia.AltText,
				tempMedia.DisplayOrder, tempMedia.IsPrimary,
				tempMedia.PublicId, tempMedia.FileSize, tempMedia.MimeType);

			await _unitOfWork.Media.AddAsync(media);
			_unitOfWork.TemporaryMedia.Remove(tempMedia);

			return (true, null);
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
		#endregion Private Helpers
	}
}
