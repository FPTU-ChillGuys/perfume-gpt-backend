using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		#region Dependencies

		private readonly IProductRepository _productRepo;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly IMapper _mapper;
		private readonly MediaBulkActionHelper _helper;
		private readonly IProductAttributeService _productAttributeService;

		public ProductService(
			IProductRepository productRepo,
			IMediaService mediaService,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			IMapper mapper,
			MediaBulkActionHelper helper,
			IProductAttributeService productAttributeService)
		{
			_productRepo = productRepo;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
			_helper = helper;
			_productAttributeService = productAttributeService;
		}

		#endregion Dependencies

		public async Task<BaseResponse<BulkActionResult<string>>> CreateProductAsync(CreateProductRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			// Validate and apply attributes using ProductAttributeService
			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. attributeErrors]
				);
			}
		var product = _mapper.Map<Product>(request);

			_productAttributeService.ApplyAttributesToProductEntity(product, request.Attributes);

			await _productRepo.AddAsync(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Failed to create product", ResponseErrorType.InternalError);
			}

			var metadata = new BulkActionMetadata();
			// Convert temporary media to permanent media if provided
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, product.Id);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			// Generate embeddings after product is fully saved with attributes and media
			await _productRepo.AddProductEmbeddingsByIdAsync(product.Id);

			var result = new BulkActionResult<string>(product.Id.ToString(), metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Product created successfully with {metadata.TotalFailed} media upload failure(s)."
				: "Product created successfully";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<BulkActionResult<string>>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Cannot update deleted product", ResponseErrorType.BadRequest);
			}

			// Prepare metadata for bulk operations
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
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIdsToAdd, productId);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			// Validate attributes
			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. attributeErrors]
				);
			}

			_mapper.Map(request, product);

			// Replace attributes using service (this will remove existing and add new ones)
			if (request.Attributes != null)
			{
				await _productAttributeService.ReplaceAttributesAsync(productId, request.Attributes, isVariant: false);
			}

		_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Failed to update product", ResponseErrorType.InternalError);
			}

			// Regenerate embeddings after product data is fully updated
			await _productRepo.AddProductEmbeddingsByIdAsync(productId);

			var result = new BulkActionResult<string>(productId.ToString(), metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Product updated successfully with {metadata.TotalFailed} media operation failure(s)."
				: "Product updated successfully";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteProductAsync(Guid productId)
		{
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<string>.Fail("Product already deleted", ResponseErrorType.BadRequest);
			}

			await _productAttributeService.RemoveAttributesByEntityIdAsync(productId, isVariant: false);
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Product, productId);
			_productRepo.Remove(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			var response = await _productRepo.GetProductResponseAsync(productId);

			if (response == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductListItemsAsync(request);

			var pagedResult = new PagedResult<ProductListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Products retrieved successfully");
		}

		public async Task<BaseResponse<List<ProductLookupItem>>> GetProductLookupListAsync()
		{
			var lookupList = await _productRepo.GetProductLookupListAsync();
			return BaseResponse<List<ProductLookupItem>>.Ok(lookupList, "Product lookup list retrieved successfully");
		}

		#region Media Management

		public async Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId)
		{
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			var result = await _mediaService.GetMediaByEntityAsync(EntityType.Product, productId);
			return result;
		}

		#endregion

		#region Semantic Search

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetSemanticSearchProductAsync(string searchText, GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductsWithSemanticSearch(searchText, request);

			var productList = _mapper.Map<List<ProductListItem>>(items ?? new List<Product>());

			var pagedResult = new PagedResult<ProductListItem>(
				productList,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Semantic search products retrieved successfully");
		}

		public async Task<BaseResponse> UpdateAllProductsEmbeddingAsync()
		{
			await _productRepo.AddAllProductEmbeddingsAsync();
			return BaseResponse.Ok("All product embeddings updated successfully");
		}

		public async Task<BaseResponse> UpdateProductEmbeddingAsync(Guid productId)
		{
			await _productRepo.AddProductEmbeddingsByIdAsync(productId);
			return BaseResponse.Ok("Product embedding updated successfully");
		}

		#endregion

		#region Private Methods

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
			List<Guid> temporaryMediaIds,
			Guid productId)
		{
			return await _helper.ConvertTemporaryMediaToPermanentAsync(
				temporaryMediaIds,
				EntityType.Product,
				productId);
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			return await _helper.DeleteMultipleMediaAsync(mediaIds);
		}

		#endregion
	}
}
