using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VariantService : IVariantService
	{
		private readonly IVariantRepository _variantRepository;
		private readonly IMediaService _mediaService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateVariantRequest> _createVariantValidator;
		private readonly IValidator<UpdateVariantRequest> _updateVariantValidator;
		private readonly IMapper _mapper;

		public VariantService(
			IVariantRepository variantRepository,
			IMediaService mediaService,
			IUnitOfWork unitOfWork,
			IValidator<CreateVariantRequest> createVariantValidator,
			IValidator<UpdateVariantRequest> updateVariantValidator,
			IMapper mapper)
		{
			_variantRepository = variantRepository;
			_mediaService = mediaService;
			_unitOfWork = unitOfWork;
			_createVariantValidator = createVariantValidator;
			_updateVariantValidator = updateVariantValidator;
			_mapper = mapper;
		}

	public async Task<BaseResponse<BulkActionResult<string>>> CreateVariantAsync(CreateVariantRequest request)
	{
		var validationResult = await _createVariantValidator.ValidateAsync(request);
		if (!validationResult.IsValid)
		{
			return BaseResponse<BulkActionResult<string>>.Fail(
				"Validation failed",
				ResponseErrorType.BadRequest,
				[.. validationResult.Errors.Select(e => e.ErrorMessage)]
			);
		}

		// Use mapper to create ProductVariant entity
		var variant = _mapper.Map<ProductVariant>(request);

		await _variantRepository.AddAsync(variant);
		var saved = await _variantRepository.SaveChangesAsync();

		if (!saved)
		{
			return BaseResponse<BulkActionResult<string>>.Fail("Failed to create variant", ResponseErrorType.InternalError);
		}

		var metadata = new BulkActionMetadata();
		// Convert temporary media to permanent if provided
		if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
		{
			var uploadResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, variant.Id);
			if (uploadResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", uploadResult));
			}
		}

		var result = new BulkActionResult<string>(variant.Id.ToString(), metadata.Operations.Count > 0 ? metadata : null);
		var message = metadata.HasPartialFailure
			? $"Variant created successfully with {metadata.TotalFailed} media upload failure(s)."
			: "Variant created successfully";

		return BaseResponse<BulkActionResult<string>>.Ok(result, message);
	}

	public async Task<BaseResponse<BulkActionResult<string>>> UpdateVariantAsync(Guid variantId, UpdateVariantRequest request)
	{
		var validationResult = await _updateVariantValidator.ValidateAsync(request);
		if (!validationResult.IsValid)
		{
			return BaseResponse<BulkActionResult<string>>.Fail(
				"Validation failed",
				ResponseErrorType.BadRequest,
				validationResult.Errors.Select(e => e.ErrorMessage).ToList()
			);
		}

		var variant = await _variantRepository.GetByIdAsync(variantId);
		if (variant == null)
		{
			return BaseResponse<BulkActionResult<string>>.Fail("Variant not found", ResponseErrorType.NotFound);
		}

		if (variant.IsDeleted)
		{
			return BaseResponse<BulkActionResult<string>>.Fail("Cannot update deleted variant", ResponseErrorType.BadRequest);
		}

		// Use mapper to update entity
		_mapper.Map(request, variant);

		_variantRepository.Update(variant);
		var saved = await _variantRepository.SaveChangesAsync();

		if (!saved)
		{
			return BaseResponse<BulkActionResult<string>>.Fail("Failed to update variant", ResponseErrorType.InternalError);
		}

		// === IMAGE MANAGEMENT ===
		var metadata = new BulkActionMetadata();

		// Delete specified images first
		if (request.MediaIdsToDelete != null && request.MediaIdsToDelete.Count != 0)
		{
			var deleteResult = await DeleteMultipleMediaAsync(request.MediaIdsToDelete);
			if (deleteResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Deletion", deleteResult));
			}
		}

		// Add new images from temporary media
		if (request.TemporaryMediaIdsToAdd != null && request.TemporaryMediaIdsToAdd.Count != 0)
		{
			var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIdsToAdd, variantId);
			if (conversionResult.TotalProcessed > 0)
			{
				metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
			}
		}

		var result = new BulkActionResult<string>(variantId.ToString(), metadata.Operations.Count > 0 ? metadata : null);
		var message = metadata.HasPartialFailure
			? $"Variant updated successfully with {metadata.TotalFailed} media operation failure(s)."
			: "Variant updated successfully";

		return BaseResponse<BulkActionResult<string>>.Ok(result, message);
	}

		public async Task<BaseResponse<string>> DeleteVariantAsync(Guid variantId)
		{
			var variant = await _variantRepository.GetByIdAsync(variantId);
			if (variant == null)
			{
				return BaseResponse<string>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			if (variant.IsDeleted)
			{
				return BaseResponse<string>.Fail("Variant already deleted", ResponseErrorType.BadRequest);
			}

			// Delete all media for this variant
			var deleteMediaResult = await _mediaService.DeleteAllMediaByEntityAsync(EntityType.ProductVariant, variantId);
			if (!deleteMediaResult.Success)
			{
				Console.WriteLine($"Warning: Failed to delete media for variant {variantId}: {deleteMediaResult.Message}");
			}

			_variantRepository.Remove(variant);
			var saved = await _variantRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete variant", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(variantId.ToString(), "Variant deleted successfully");
		}

		public async Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsAsync(GetPagedVariantsRequest request)
		{
			var (items, totalCount) = await _variantRepository.GetPagedVariantsWithDetailsAsync(request);

			var variantList = _mapper.Map<List<VariantPagedItem>>(items);

			var pagedResult = new PagedResult<VariantPagedItem>(
				variantList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<VariantPagedItem>>.Ok(pagedResult, "Variants retrieved successfully");
		}

		public async Task<BaseResponse<ProductVariantResponse>> GetVariantByIdAsync(Guid variantId)
		{
			var variant = await _variantRepository.GetVariantWithDetailsAsync(variantId);

			if (variant == null)
			{
				return BaseResponse<ProductVariantResponse>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<ProductVariantResponse>(variant);

			return BaseResponse<ProductVariantResponse>.Ok(response, "Variant retrieved successfully");
		}

		public async Task<BaseResponse<ProductVariantResponse>> GetVariantByBarcodeAsync(string barcode)
		{
			var variant = await _variantRepository.GetByBarcodeAsync(barcode);

			if (variant == null)
			{
				return BaseResponse<ProductVariantResponse>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<ProductVariantResponse>(variant);

			return BaseResponse<ProductVariantResponse>.Ok(response, "Variant retrieved successfully");
		}

		public async Task<BaseResponse<List<VariantLookupItem>>> GetVariantLookupListAsync(Guid? productId = null)
		{
			var query = await _variantRepository.GetAllAsync(
				filter: v => !v.IsDeleted && v.Status != VariantStatus.Discontinued
					&& (!productId.HasValue || v.ProductId == productId.Value),
				include: q => q.Include(v => v.Concentration)
					.Include(v => v.Product)
					.Include(v => v.Media.Where(m => !m.IsDeleted && m.IsPrimary))
			);

			var variants = query.OrderBy(v => v.Sku).ToList();

			// Use mapper to convert to VariantLookupItem
			var lookupItems = _mapper.Map<List<VariantLookupItem>>(variants);

			return BaseResponse<List<VariantLookupItem>>.Ok(lookupItems, "Variant lookup list retrieved successfully");
		}

		public (bool IsValid, string? ErrorMessage) ValidateVariantForCart(ProductVariant variant)
		{
			if (variant.IsDeleted)
			{
				return (false, "This product variant is no longer available");
			}

			if (variant.Status == VariantStatus.Discontinued)
			{
				return (false, "This product variant has been discontinued");
			}

			if (variant.Status == VariantStatus.Inactive)
			{
				return (false, "This product variant is currently inactive");
			}

			if (variant.Status == VariantStatus.out_of_stock)
			{
				return (false, "This product variant is out of stock");
			}

			return (true, null);
		}

		#region Media Management

		public async Task<BaseResponse<List<MediaResponse>>> GetVariantImagesAsync(Guid variantId)
		{
			// Verify variant exists
			var variant = await _variantRepository.GetByIdAsync(variantId);
			if (variant == null)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			// Get media
			var result = await _mediaService.GetMediaByEntityAsync(EntityType.ProductVariant, variantId);
			return result;
		}

		#endregion

		#region Private Methods

	/// <summary>
	/// Converts temporary media list to permanent with error tracking (used in Create/Update)
	/// </summary>
	private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(List<Guid> temporaryMediaIds, Guid variantId)
	{
		var response = new BulkActionResponse();

		foreach (var tempMediaId in temporaryMediaIds)
		{
			try
			{
				// Get temporary media
				var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
				if (tempMedia == null)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = tempMediaId,
						ErrorMessage = "Temporary media not found"
					});
					continue;
				}

				if (tempMedia.IsExpired)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = tempMediaId,
						ErrorMessage = "Temporary media has expired"
					});
					continue;
				}

				// Create permanent media from temporary
				var media = new Media
				{
					Url = tempMedia.Url,
					AltText = tempMedia.AltText,
					EntityType = EntityType.ProductVariant,
					ProductVariantId = variantId,
					DisplayOrder = tempMedia.DisplayOrder,
					IsPrimary = tempMedia.IsPrimary, // Use the IsPrimary from temp media
					PublicId = tempMedia.PublicId,
					FileSize = tempMedia.FileSize,
					MimeType = tempMedia.MimeType,
				};

				await _unitOfWork.Media.AddAsync(media);
				_unitOfWork.TemporaryMedia.Remove(tempMedia);

				response.SucceededIds.Add(tempMediaId);
			}
			catch (Exception ex)
			{
				response.FailedItems.Add(new BulkActionError
				{
					Id = tempMediaId,
					ErrorMessage = $"Failed to convert media: {ex.Message}"
				});
			}
		}

		if (response.SucceededIds.Count > 0)
		{
			await _unitOfWork.SaveChangesAsync();
		}

		return response;
	}

	private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
	{
		var response = new BulkActionResponse();

		foreach (var mediaId in mediaIds)
		{
			try
			{
				var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
				if (deleteResult.Success)
				{
					response.SucceededIds.Add(mediaId);
				}
				else
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = mediaId,
						ErrorMessage = deleteResult.Message ?? "Unknown error"
					});
				}
			}
			catch (Exception ex)
			{
				response.FailedItems.Add(new BulkActionError
				{
					Id = mediaId,
					ErrorMessage = $"Exception during deletion: {ex.Message}"
				});
			}
		}

		return response;
	}

		#endregion
	}
}
