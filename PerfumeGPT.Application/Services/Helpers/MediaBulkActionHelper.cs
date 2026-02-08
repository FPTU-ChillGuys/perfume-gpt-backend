using MapsterMapper;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers
{
	public class MediaBulkActionHelper
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IMediaService _mediaService;

		public MediaBulkActionHelper(IUnitOfWork unitOfWork, IMapper mapper, IMediaService mediaService)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_mediaService = mediaService;
		}

		private async Task<BulkActionResponse> ProcessBulkActionAsync<T>(
			List<T> items,
			Func<T, Task<(bool success, string? errorMessage)>> processItem,
			bool saveChanges = false)
		{
			var response = new BulkActionResponse();

			foreach (var item in items)
			{
				try
				{
					var (success, errorMessage) = await processItem(item);

					if (success)
					{
						response.SucceededIds.Add(GetId(item));
					}
					else
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = GetId(item),
							ErrorMessage = errorMessage ?? "Unknown error"
						});
					}
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = GetId(item),
						ErrorMessage = $"Exception during processing: {ex.Message}"
					});
				}
			}

			if (saveChanges && response.SucceededIds.Count > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			return response;
		}

		private static Guid GetId<T>(T item) => item switch
		{
			Guid guid => guid,
			_ => throw new ArgumentException($"Unsupported type: {typeof(T)}")
		};

		public async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
			List<Guid> temporaryMediaIds,
			EntityType entityType,
			Guid entityId)
		{
			return await ProcessBulkActionAsync(
				temporaryMediaIds,
				async (tempMediaId) => await ConvertSingleTemporaryMediaAsync(tempMediaId, entityType, entityId),
				saveChanges: true);
		}

		private async Task<(bool success, string? errorMessage)> ConvertSingleTemporaryMediaAsync(
			Guid tempMediaId,
			EntityType entityType,
			Guid entityId)
		{
			// Get temporary media
			var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
			if (tempMedia == null)
			{
				return (false, "Temporary media not found");
			}

			if (tempMedia.IsExpired)
			{
				return (false, "Temporary media has expired");
			}

			// Create permanent media
			var media = _mapper.Map<Media>(tempMedia);
			media.EntityType = entityType;

			// Set the appropriate entity ID based on type
			switch (entityType)
			{
				case EntityType.Review:
					media.ReviewId = entityId;
					break;
				case EntityType.Product:
					media.ProductId = entityId;
					break;
				case EntityType.ProductVariant:
					media.ProductVariantId = entityId;
					break;
				default:
					return (false, $"Unsupported entity type: {entityType}");
			}

			await _unitOfWork.Media.AddAsync(media);
			_unitOfWork.TemporaryMedia.Remove(tempMedia);

			return (true, null);
		}

		public async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			return await ProcessBulkActionAsync(
				mediaIds,
				async (mediaId) =>
				{
					var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
					return (deleteResult.Success, deleteResult.Message);
				});
		}
	}
}
