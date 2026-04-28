using PerfumeGPT.Application.DTOs.Requests.Pages;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Pages;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class PageService : IPageService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly MediaBulkActionHelper _mediaBulkActionHelper;

		public PageService(IUnitOfWork unitOfWork, MediaBulkActionHelper mediaBulkActionHelper)
		{
			_unitOfWork = unitOfWork;
			_mediaBulkActionHelper = mediaBulkActionHelper;
		}

		public async Task<BaseResponse<PageResponse>> CreatePageAsync(CreatePageRequest request)
		{
			var exists = await _unitOfWork.Pages.SlugExistsAsync(request.Slug);
			if (exists)
			{
				throw AppException.Conflict($"Trang với slug '{request.Slug}' đã tồn tại.");
			}

			var page = SystemPage.Create(
				request.Title,
				request.Slug,
				request.HtmlContent,
				request.IsPublished,
				request.MetaDescription);

			await _unitOfWork.Pages.AddAsync(page);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo trang thất bại.");

			var message = "Tạo trang thành công.";
			if (request.TemporaryMediaIds?.Count > 0)
			{
				var conversionResult = await _mediaBulkActionHelper.ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIds,
					EntityType.SystemPage,
					page.Id);

				if (conversionResult.TotalProcessed > 0 && conversionResult.HasError)
				{
					message = $"Tạo trang thành công nhưng có {conversionResult.FailedItems.Count} ảnh tải lên thất bại.";
				}
			}

			var pageImages = await _unitOfWork.Media.GetMediaByEntityTypeAsync(EntityType.SystemPage, page.Id);
			return BaseResponse<PageResponse>.Ok(ToResponse(page, pageImages), message);
		}

		public async Task<BaseResponse> DeletePageAsync(string slug)
		{
			var page = await _unitOfWork.Pages.GetBySlugAsync(slug)
				?? throw AppException.NotFound($"Không tìm thấy trang với slug '{slug}'.");

			await _mediaBulkActionHelper.DeleteMultipleMediaAsync(
				page.PageImages
					.Where(m => !m.IsDeleted)
					.Select(m => m.Id)
					.ToList());

			_unitOfWork.Pages.Remove(page);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa trang thất bại.");

			return BaseResponse.Ok("Xóa trang thành công.");
		}

		public async Task<BaseResponse<PageResponse>> GetPageContentAsync(string slug)
		{
			var page = await _unitOfWork.Pages.GetPublishedBySlugAsync(slug)
				?? throw AppException.NotFound($"Không tìm thấy trang với slug '{slug}'.");

			return BaseResponse<PageResponse>.Ok(ToResponse(page, page.PageImages));
		}

		public async Task<BaseResponse<PagedResult<PageResponse>>> GetPagesAsync(GetPagedPageRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Pages.GetPagedPagesAsync(request);

			var pagedResult = new PagedResult<PageResponse>(items, request.PageNumber, request.PageSize, totalCount);
			return BaseResponse<PagedResult<PageResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<string>> PublishPageAsync(string slug)
		{
			var page = await _unitOfWork.Pages.GetBySlugAsync(slug)
				?? throw AppException.NotFound($"Không tìm thấy trang với slug '{slug}'.");

			page.Publish();
			_unitOfWork.Pages.Update(page);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xuất bản trang thất bại.");

			return BaseResponse<string>.Ok(page.Slug, "Xuất bản trang thành công.");
		}

		public async Task<BaseResponse<PageResponse>> UpdatePageAsync(string slug, UpdatePageRequest request)
		{
			var page = await _unitOfWork.Pages.GetBySlugAsync(slug)
				?? throw AppException.NotFound($"Không tìm thấy trang với slug '{slug}'.");

			if (!string.Equals(page.Slug, request.Slug.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				var slugExists = await _unitOfWork.Pages.SlugExistsAsync(request.Slug, page.Id);
				if (slugExists)
				{
					throw AppException.Conflict($"Trang với slug '{request.Slug}' đã tồn tại.");
				}
			}

			page.Update(request.Title, request.Slug, request.HtmlContent, request.MetaDescription);
			_unitOfWork.Pages.Update(page);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật trang thất bại.");

			var message = "Cập nhật trang thành công.";
			var metadata = new BulkActionMetadata { Operations = [] };

			if (request.MediaIdsToDelete?.Count > 0)
			{
				var deleteResult = await _mediaBulkActionHelper.DeleteMultipleMediaAsync(request.MediaIdsToDelete);
				if (deleteResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Deletion", deleteResult));
				}
			}

			if (request.TemporaryMediaIdsToAdd?.Count > 0)
			{
				var conversionResult = await _mediaBulkActionHelper.ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIdsToAdd,
					EntityType.SystemPage,
					page.Id);

				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			if (metadata.HasPartialFailure)
			{
				message = $"Cập nhật trang thành công nhưng có {metadata.TotalFailed} thao tác media thất bại.";
			}

			var pageImages = await _unitOfWork.Media.GetMediaByEntityTypeAsync(EntityType.SystemPage, page.Id);
			return BaseResponse<PageResponse>.Ok(ToResponse(page, pageImages), message);
		}

		private static PageResponse ToResponse(SystemPage page, IEnumerable<Media>? images = null)
		{
			var mappedImages = (images ?? page.PageImages ?? [])
				.Where(m => !m.IsDeleted)
				.Select(m => new MediaResponse
				{
					Id = m.Id,
					Url = m.Url,
					AltText = m.AltText,
					DisplayOrder = m.DisplayOrder,
					IsPrimary = m.IsPrimary,
					FileSize = m.FileSize,
					MimeType = m.MimeType
				})
				.ToList();

			return new PageResponse
			{
				Slug = page.Slug,
				Title = page.Title,
				HtmlContent = page.HtmlContent,
				IsPublished = page.IsPublished,
				MetaDescription = page.MetaDescription,
				Images = mappedImages,
				UpdatedAt = page.UpdatedAt ?? page.CreatedAt
			};
		}
	}
}
