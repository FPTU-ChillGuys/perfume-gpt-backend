using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VariantService : IVariantService
	{
		#region Dependencies

		private readonly IVariantRepository _variantRepository;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateVariantRequest> _createVariantValidator;
		private readonly IValidator<UpdateVariantRequest> _updateVariantValidator;
		private readonly IMapper _mapper;
		private readonly MediaBulkActionHelper _helper;
		private readonly IProductAttributeService _productAttributeService;

		public VariantService(
			IVariantRepository variantRepository,
			IMediaService mediaService,
			IValidator<CreateVariantRequest> createVariantValidator,
			IValidator<UpdateVariantRequest> updateVariantValidator,
			IMapper mapper,
			MediaBulkActionHelper helper,
			IProductAttributeService productAttributeService)
		{
			_variantRepository = variantRepository;
			_mediaService = mediaService;
			_createVariantValidator = createVariantValidator;
			_updateVariantValidator = updateVariantValidator;
			_mapper = mapper;
			_helper = helper;
			_productAttributeService = productAttributeService;
		}

		#endregion Dependencies

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

			var variant = _mapper.Map<ProductVariant>(request);

			// Validate variant attributes
			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes, isForVariant: true);
			if (attributeErrors.Count != 0)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. attributeErrors]
				);
			}

			_productAttributeService.ApplyAttributesToVariantEntity(variant, request.Attributes);

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
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
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

			_mapper.Map(request, variant);

			// Validate variant attributes
			var updateAttributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes, isForVariant: true);
			if (updateAttributeErrors.Count != 0)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					updateAttributeErrors
				);
			}

			// Replace variant attributes
			if (request.Attributes != null)
			{
				await _productAttributeService.ReplaceAttributesAsync(variantId, request.Attributes, isVariant: true);
			}

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

			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.ProductVariant, variantId);
			await _productAttributeService.RemoveAttributesByEntityIdAsync(variantId, isVariant: true);
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

			var pagedResult = new PagedResult<VariantPagedItem>(
				items,
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

			return BaseResponse<ProductVariantResponse>.Ok(variant, "Variant retrieved successfully");
		}

		public async Task<VariantCreateOrder?> GetVariantForCreateOrderAsync(Guid variantId)
		{
			return await _variantRepository.GetVariantForCreateOrderAsync(variantId);
		}

		public async Task<BaseResponse<ProductVariantResponse>> GetVariantByBarcodeAsync(string barcode)
		{
			var variant = await _variantRepository.GetByBarcodeAsync(barcode);

			if (variant == null)
			{
				return BaseResponse<ProductVariantResponse>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			return BaseResponse<ProductVariantResponse>.Ok(variant, "Variant retrieved successfully");
		}

		public async Task<BaseResponse<List<VariantLookupItem>>> GetVariantLookupListAsync(Guid? productId = null)
		{

			var lookupItems = await _variantRepository.GetLookupList(productId);
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

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
			List<Guid> temporaryMediaIds,
			Guid variantId)
		{
			return await _helper.ConvertTemporaryMediaToPermanentAsync(
				temporaryMediaIds,
				EntityType.ProductVariant,
				variantId);
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			return await _helper.DeleteMultipleMediaAsync(mediaIds);
		}

		#endregion
	}
}
