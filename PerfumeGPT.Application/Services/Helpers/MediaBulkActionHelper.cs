using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;
using static PerfumeGPT.Domain.Entities.Media;

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
			EntityType.OrderReturnRequest,
			EntityType.Banner,
			EntityType.SystemPage
		];

		public MediaBulkActionHelper(IUnitOfWork unitOfWork, ISupabaseService supabaseService)
		{
			_unitOfWork = unitOfWork;
			_supabaseService = supabaseService;
		}

		public async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(List<Guid> temporaryMediaIds, EntityType entityType, Guid entityId)
		{
			if (!SupportedEntityTypes.Contains(entityType))
				throw AppException.BadRequest($"Không hỗ trợ loại thực thể {entityType}.");

			var response = new BulkActionResponse { SucceededIds = [], FailedItems = [] };

			// 1. BULK READ: Kéo tất cả TempMedia lên bằng 1 Query
			var tempMedias = await _unitOfWork.TemporaryMedia.GetAllAsync(m => temporaryMediaIds.Contains(m.Id));
			var tempMediaDict = tempMedias.ToDictionary(m => m.Id);

			var mediaToAdd = new List<Media>();
			var tempMediaToRemove = new List<TemporaryMedia>();

			foreach (var tempMediaId in temporaryMediaIds)
			{
				if (!tempMediaDict.TryGetValue(tempMediaId, out var tempMedia))
				{
					response.FailedItems.Add(new BulkActionError { Id = tempMediaId, ErrorMessage = "Không tìm thấy media tạm thời." });
					continue;
				}

				try
				{
					tempMedia.EnsureNotExpired();
				}
				catch (DomainException ex)
				{
					response.FailedItems.Add(new BulkActionError { Id = tempMediaId, ErrorMessage = ex.Message });
					continue;
				}

				var fileMetadata = new FileMetadata { Url = tempMedia.Url, PublicId = tempMedia.PublicId, FileSize = tempMedia.FileSize, MimeType = tempMedia.MimeType };
				var displayInfo = entityType == EntityType.SystemPage
					? new MediaDisplayInfo { AltText = tempMedia.AltText, DisplayOrder = 0, IsPrimary = false }
					: new MediaDisplayInfo { AltText = tempMedia.AltText, DisplayOrder = tempMedia.DisplayOrder, IsPrimary = tempMedia.IsPrimary };

				mediaToAdd.Add(Media.Create(entityType, entityId, fileMetadata, displayInfo));
				tempMediaToRemove.Add(tempMedia);
				response.SucceededIds.Add(tempMediaId);
			}

			// 3. BULK WRITE
			if (mediaToAdd.Count > 0)
			{
				await _unitOfWork.Media.AddRangeAsync(mediaToAdd);
				_unitOfWork.TemporaryMedia.RemoveRange(tempMediaToRemove);
				await _unitOfWork.SaveChangesAsync();
			}

			return response;
		}

		public async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			var response = new BulkActionResponse { SucceededIds = [], FailedItems = [] };

			// 1. BULK READ
			var medias = await _unitOfWork.Media.GetAllAsync(m => mediaIds.Contains(m.Id));
			var mediaDict = medias.ToDictionary(m => m.Id);

			var deleteSupabaseTasks = new List<Task>();
			var mediaToRemove = new List<Media>();

			foreach (var mediaId in mediaIds)
			{
				if (!mediaDict.TryGetValue(mediaId, out var media) || media.IsDeleted)
				{
					response.FailedItems.Add(new BulkActionError { Id = mediaId, ErrorMessage = "Không tìm thấy media hoặc media đã bị xóa." });
					continue;
				}

				if (media.EntityType != EntityType.SystemPage)
				{
					try { media.EnsureNotPrimary(); }
					catch (Exception ex)
					{
						response.FailedItems.Add(new BulkActionError { Id = mediaId, ErrorMessage = ex.Message });
						continue;
					}
				}

				// 2. PARALLEL HTTP CALLS PREPARATION: Gom các Task xóa ảnh lại, KHÔNG await ở đây
				if (!string.IsNullOrEmpty(media.PublicId))
				{
					deleteSupabaseTasks.Add(_supabaseService.DeleteImageAsync(media.Url, GetBucketName(media.EntityType)));
				}

				mediaToRemove.Add(media);
				response.SucceededIds.Add(mediaId);
			}

			// 3. EXECUTE PARALLEL & BULK WRITE
			if (response.SucceededIds.Count > 0)
			{
				// Chạy đồng thời TẤT CẢ các HTTP Request tới Supabase
				if (deleteSupabaseTasks.Count > 0) await Task.WhenAll(deleteSupabaseTasks);

				_unitOfWork.Media.RemoveRange(mediaToRemove);
				await _unitOfWork.SaveChangesAsync();
			}

			return response;
		}

		private static string GetBucketName(EntityType entityType) => entityType switch
		{
			EntityType.Product => "Products",
			EntityType.ProductVariant => "ProductVariants",
			EntityType.User => "ProfileAvatars",
			EntityType.Review => "Reviews",
			EntityType.Banner => "Banners",
			EntityType.OrderReturnRequest => "OrderReturnRequests",
			EntityType.SystemPage => "SystemPages",
			_ => "Products"
		};
	}
}
