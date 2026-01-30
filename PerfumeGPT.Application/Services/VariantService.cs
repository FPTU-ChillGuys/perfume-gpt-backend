using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VariantService : IVariantService
	{
		private readonly IVariantRepository _variantRepository;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateVariantRequest> _createVariantValidator;
		private readonly IValidator<UpdateVariantRequest> _updateVariantValidator;
		private readonly IMapper _mapper;

		public VariantService(
			IVariantRepository variantRepository,
			IMediaService mediaService,
			IValidator<CreateVariantRequest> createVariantValidator,
			IValidator<UpdateVariantRequest> updateVariantValidator,
			IMapper mapper)
		{
			_variantRepository = variantRepository;
			_mediaService = mediaService;
			_createVariantValidator = createVariantValidator;
			_updateVariantValidator = updateVariantValidator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<string>> CreateVariantAsync(CreateVariantRequest request, FileUpload? imageFile)
		{
			var validationResult = await _createVariantValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			// Use mapper to create ProductVariant entity
			var variant = _mapper.Map<ProductVariant>(request);

			await _variantRepository.AddAsync(variant);
			var saved = await _variantRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to create variant", ResponseErrorType.InternalError);
			}

			// Upload image if provided using MediaService
			if (imageFile != null && imageFile.Length > 0)
			{
				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					// Variant created but image validation failed - log warning
					Console.WriteLine($"Warning: Invalid image file type for variant {variant.Id}");
					return BaseResponse<string>.Ok(variant.Id.ToString(), "Variant created successfully but image upload failed: Invalid file type");
				}

				// Validate file size (max 5MB)
				if (imageFile.Length > 5 * 1024 * 1024)
				{
					Console.WriteLine($"Warning: Image file too large for variant {variant.Id}");
					return BaseResponse<string>.Ok(variant.Id.ToString(), "Variant created successfully but image upload failed: File too large");
				}

				var mediaResult = await _mediaService.UploadMediaAsync(
					imageFile.FileStream,
					imageFile.FileName,
					EntityType.ProductVariant,
					variant.Id,
					altText: null,
					displayOrder: 0,
					isPrimary: true
				);

				if (!mediaResult.Success)
				{
					// Variant was created but image upload failed - log warning
					Console.WriteLine($"Warning: Variant {variant.Id} created but image upload failed: {mediaResult.Message}");
					return BaseResponse<string>.Ok(variant.Id.ToString(), "Variant created successfully but image upload failed");
				}
			}

			return BaseResponse<string>.Ok(variant.Id.ToString(), "Variant created successfully");
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

			variant.IsDeleted = true;
			variant.DeletedAt = DateTime.UtcNow;

			_variantRepository.Update(variant);
			var saved = await _variantRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete variant", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(variantId.ToString(), "Variant deleted successfully");
		}

		public async Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsAsync(GetPagedVariantsRequest request)
		{
			// Use repository method with includes
			var (items, totalCount) = await _variantRepository.GetPagedVariantsWithDetailsAsync(request);

			// Use mapper to convert to response DTOs
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
			// Use repository method with includes
			var variant = await _variantRepository.GetVariantWithDetailsAsync(variantId);

			if (variant == null)
			{
				return BaseResponse<ProductVariantResponse>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			// Use mapper to convert to response DTO
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

			// Use mapper to convert to response DTO
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

		public async Task<BaseResponse<string>> UpdateVariantAsync(Guid variantId, UpdateVariantRequest request, FileUpload? imageFile)
		{
			var validationResult = await _updateVariantValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			var variant = await _variantRepository.GetByIdAsync(variantId);
			if (variant == null)
			{
				return BaseResponse<string>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			if (variant.IsDeleted)
			{
				return BaseResponse<string>.Fail("Cannot update deleted variant", ResponseErrorType.BadRequest);
			}

			// Use mapper to update entity
			_mapper.Map(request, variant);

			_variantRepository.Update(variant);
			var saved = await _variantRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to update variant", ResponseErrorType.InternalError);
			}

			// Upload new image if provided
			if (imageFile != null && imageFile.Length > 0)
			{
				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					Console.WriteLine($"Warning: Invalid image file type for variant {variantId}");
					return BaseResponse<string>.Ok(variantId.ToString(), "Variant updated successfully but image upload failed: Invalid file type");
				}

				// Validate file size (max 5MB)
				if (imageFile.Length > 5 * 1024 * 1024)
				{
					Console.WriteLine($"Warning: Image file too large for variant {variantId}");
					return BaseResponse<string>.Ok(variantId.ToString(), "Variant updated successfully but image upload failed: File too large");
				}

			// Get existing primary image to delete after new upload succeeds
			var existingMediaResult = await _mediaService.GetMediaByEntityAsync(EntityType.ProductVariant, variantId);
			var oldPrimaryMedia = existingMediaResult.Payload?.FirstOrDefault(m => m.IsPrimary);

				// Upload new image
				var mediaResult = await _mediaService.UploadMediaAsync(
					imageFile.FileStream,
					imageFile.FileName,
					EntityType.ProductVariant,
					variantId,
					altText: null,
					displayOrder: 0,
					isPrimary: true
				);

				if (!mediaResult.Success)
				{
					Console.WriteLine($"Warning: Image upload failed for variant {variantId}: {mediaResult.Message}");
					return BaseResponse<string>.Ok(variantId.ToString(), "Variant updated successfully but image upload failed");
				}

				// Delete old primary image if it exists
				if (oldPrimaryMedia != null)
				{
					var deleteResult = await _mediaService.DeleteMediaAsync(oldPrimaryMedia.Id);
					if (!deleteResult.Success)
					{
						Console.WriteLine($"Warning: Failed to delete old primary image for variant {variantId}");
					}
				}
			}

			return BaseResponse<string>.Ok(variantId.ToString(), "Variant updated successfully");
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
	}
}
