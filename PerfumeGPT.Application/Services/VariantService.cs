using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class VariantService : IVariantService
	{
		private readonly IVariantRepository _variantRepository;
		private readonly ISupabaseService _supabaseService;
		private readonly IValidator<CreateVariantRequest> _createVariantValidator;
		private readonly IValidator<UpdateVariantRequest> _updateVariantValidator;

		public VariantService(IVariantRepository variantRepository, IValidator<CreateVariantRequest> createVariantValidator, IValidator<UpdateVariantRequest> updateVariantValidator, ISupabaseService supabaseService)
		{
			_variantRepository = variantRepository;
			_createVariantValidator = createVariantValidator;
			_updateVariantValidator = updateVariantValidator;
			_supabaseService = supabaseService;
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

		string? imageUrl = null;

		// Upload image if provided
		if (imageFile != null && imageFile.Length > 0)
		{
			// Validate file type
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return BaseResponse<string>.Fail(
					"Invalid file type. Only images (jpg, jpeg, png, gif, webp) are allowed.",
					ResponseErrorType.BadRequest
				);
			}

			// Validate file size (max 5MB)
			if (imageFile.Length > 5 * 1024 * 1024)
			{
				return BaseResponse<string>.Fail(
					"File size exceeds 5MB limit.",
					ResponseErrorType.BadRequest
				);
			}

			imageUrl = await _supabaseService.UploadVariantImageAsync(imageFile.FileStream, imageFile.FileName);

			if (string.IsNullOrWhiteSpace(imageUrl))
			{
				return BaseResponse<string>.Fail(
					"Failed to upload image to storage.",
					ResponseErrorType.InternalError
				);
			}
		}

		var variant = new ProductVariant
		{
			ProductId = request.ProductId,
			ImageUrl = imageUrl ?? request.ImageUrl,
			Sku = request.Sku,
			VolumeMl = request.VolumeMl,
			ConcentrationId = request.ConcentrationId,
			Type = request.Type,
			BasePrice = request.BasePrice,
			Status = request.Status,
			CreatedAt = DateTime.UtcNow,
			IsDeleted = false
		};

		await _variantRepository.AddAsync(variant);
		var saved = await _variantRepository.SaveChangesAsync();

		if (!saved)
		{
			// Cleanup: delete uploaded image if variant creation failed
			if (!string.IsNullOrWhiteSpace(imageUrl))
			{
				await _supabaseService.DeleteVariantImageAsync(imageUrl);
			}
			return BaseResponse<string>.Fail("Failed to create variant", ResponseErrorType.InternalError);
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

		// Delete image from Supabase if exists
		if (!string.IsNullOrWhiteSpace(variant.ImageUrl))
		{
			var imageDeleted = await _supabaseService.DeleteVariantImageAsync(variant.ImageUrl);
			if (!imageDeleted)
			{
				Console.WriteLine($"Warning: Failed to delete image for variant {variantId}: {variant.ImageUrl}");
			}
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
			var (items, totalCount) = await _variantRepository.GetPagedAsync(
				filter: v => !v.IsDeleted,
				include: q => q.Include(v => v.Concentration),
				orderBy: q => q.OrderByDescending(v => v.CreatedAt),
				pageNumber: request.PageNumber,
				pageSize: request.PageSize,
				asNoTracking: true
			);

			var variantList = items.Select(v => new VariantPagedItem
			{
				Id = v.Id,
				ProductId = v.ProductId,
				ImageUrl = v.ImageUrl,
				Sku = v.Sku,
				VolumeMl = v.VolumeMl,
				ConcentrationId = v.ConcentrationId,
				ConcentrationName = v.Concentration.Name ?? string.Empty,
				Type = v.Type,
				BasePrice = v.BasePrice,
				Status = v.Status
			}).ToList();

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
			var variant = await _variantRepository.GetByConditionAsync(
				v => v.Id == variantId && !v.IsDeleted,
				include: q => q.Include(v => v.Concentration),
				asNoTracking: true
			);

			if (variant == null)
			{
				return BaseResponse<ProductVariantResponse>.Fail("Variant not found", ResponseErrorType.NotFound);
			}

			var response = new ProductVariantResponse
			{
				Id = variant.Id,
				ProductId = variant.ProductId,
				Sku = variant.Sku,
				VolumeMl = variant.VolumeMl,
				ConcentrationId = variant.ConcentrationId,
				ConcentrationName = variant.Concentration.Name ?? string.Empty,
				Type = variant.Type,
				BasePrice = variant.BasePrice,
				Status = variant.Status
			};

			return BaseResponse<ProductVariantResponse>.Ok(response, "Variant retrieved successfully");
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

		string? newImageUrl = null;
		string? oldImageUrl = variant.ImageUrl;

		// Upload new image if provided
		if (imageFile != null && imageFile.Length > 0)
		{
			// Validate file type
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return BaseResponse<string>.Fail(
					"Invalid file type. Only images (jpg, jpeg, png, gif, webp) are allowed.",
					ResponseErrorType.BadRequest
				);
			}

			// Validate file size (max 5MB)
			if (imageFile.Length > 5 * 1024 * 1024)
			{
				return BaseResponse<string>.Fail(
					"File size exceeds 5MB limit.",
					ResponseErrorType.BadRequest
				);
			}

			newImageUrl = await _supabaseService.UploadVariantImageAsync(imageFile.FileStream, imageFile.FileName);

			if (string.IsNullOrWhiteSpace(newImageUrl))
			{
				return BaseResponse<string>.Fail(
					"Failed to upload new image to storage.",
					ResponseErrorType.InternalError
				);
			}
		}

		// Update variant properties
		variant.ImageUrl = newImageUrl ?? request.ImageUrl ?? variant.ImageUrl;
		variant.Sku = request.Sku;
		variant.VolumeMl = request.VolumeMl;
		variant.ConcentrationId = request.ConcentrationId;
		variant.Type = request.Type;
		variant.BasePrice = request.BasePrice;
		variant.Status = request.Status;
		variant.UpdatedAt = DateTime.UtcNow;

		_variantRepository.Update(variant);
		var saved = await _variantRepository.SaveChangesAsync();

		if (!saved)
		{
			// Cleanup: delete new image if update failed
			if (!string.IsNullOrWhiteSpace(newImageUrl))
			{
				await _supabaseService.DeleteVariantImageAsync(newImageUrl);
			}
			return BaseResponse<string>.Fail("Failed to update variant", ResponseErrorType.InternalError);
		}

		// Delete old image if a new one was uploaded successfully
		if (!string.IsNullOrWhiteSpace(newImageUrl) && !string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl != newImageUrl)
		{
			var oldImageDeleted = await _supabaseService.DeleteVariantImageAsync(oldImageUrl);
			if (!oldImageDeleted)
			{
				Console.WriteLine($"Warning: Failed to delete old image: {oldImageUrl}");
			}
		}

		return BaseResponse<string>.Ok(variantId.ToString(), "Variant updated successfully");
	}
	}
}
