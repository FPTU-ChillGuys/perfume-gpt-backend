using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly IProductRepository _productRepo;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly IMapper _mapper;

		public ProductService(
			IProductRepository productRepo,
			IMediaService mediaService,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			IMapper mapper)
		{
			_productRepo = productRepo;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<string>> CreateProductAsync(CreateProductRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			// Use mapper to create Product entity
			var product = _mapper.Map<Product>(request);

			await _productRepo.AddAsync(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to create product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(product.Id.ToString(), "Product created successfully");
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

			_productRepo.Remove(product);
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Product, productId);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			// Use repository method with includes
			var product = await _productRepo.GetProductWithDetailsAsync(productId);

			if (product == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Use mapper to convert to response DTO
			var response = _mapper.Map<ProductResponse>(product);

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			// Use repository method with includes
			var (items, totalCount) = await _productRepo.GetPagedProductsWithDetailsAsync(request);

			// Use mapper to convert to response DTOs
			var productList = _mapper.Map<List<ProductListItem>>(items);

			var pagedResult = new PagedResult<ProductListItem>(
				productList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Products retrieved successfully");
		}

		public async Task<BaseResponse<string>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<string>.Fail("Cannot update deleted product", ResponseErrorType.BadRequest);
			}

			// Use mapper to update entity
			_mapper.Map(request, product);

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to update product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product updated successfully");
		}

		// Media management methods
		public async Task<BaseResponse<List<MediaResponse>>> UploadProductImageAsync(Guid productId, BulkUploadMediaRequest request)
		{
			// Verify product exists
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Cannot add image to deleted product", ResponseErrorType.BadRequest);
			}

			// Validate bulk request
			if (request.Images == null || request.Images.Count == 0)
			{
				return BaseResponse<List<MediaResponse>>.Fail("At least one image is required", ResponseErrorType.BadRequest);
			}

			var uploadedMedia = new List<MediaResponse>();
			var errors = new List<string>();

			// Process each image
			foreach (var imageRequest in request.Images)
			{
				// Validate image
				if (imageRequest.ImageFile == null || imageRequest.ImageFile.Length == 0)
				{
					errors.Add($"Image file is required for one of the images");
					continue;
				}

				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(imageRequest.ImageFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(extension))
				{
					errors.Add($"Invalid image format for {imageRequest.ImageFile.FileName}. Allowed: jpg, jpeg, png, gif, webp");
					continue;
				}

				// Validate file size (max 5MB)
				const long maxFileSize = 5 * 1024 * 1024;
				if (imageRequest.ImageFile.Length > maxFileSize)
				{
					errors.Add($"Image size must be less than 5MB for {imageRequest.ImageFile.FileName}");
					continue;
				}

				// Upload image
				using var stream = imageRequest.ImageFile.OpenReadStream();
				var result = await _mediaService.UploadMediaAsync(
					stream,
					imageRequest.ImageFile.FileName,
					EntityType.Product,
					productId,
					imageRequest.AltText,
					imageRequest.DisplayOrder,
					imageRequest.IsPrimary
				);

				if (result.Success && result.Payload != null)
				{
					uploadedMedia.Add(result.Payload);
				}
				else
				{
					errors.Add($"Failed to upload {imageRequest.ImageFile.FileName}: {result.Message}");
				}
			}

			// Return response based on results
			if (uploadedMedia.Count == 0)
			{
				return BaseResponse<List<MediaResponse>>.Fail(
					"Failed to upload any images",
					ResponseErrorType.BadRequest,
					errors
				);
			}

			return BaseResponse<List<MediaResponse>>.Ok(
				uploadedMedia,
				$"Successfully uploaded {uploadedMedia.Count} image(s)"
			);
		}

		public async Task<BaseResponse<string>> DeleteProductImageAsync(Guid productId, Guid mediaId)
		{
			// Verify product exists
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Delete media
			var result = await _mediaService.DeleteMediaAsync(mediaId);
			return result;
		}

		public async Task<BaseResponse<string>> SetPrimaryProductImageAsync(Guid productId, Guid mediaId)
		{
			// Verify product exists
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Set primary media
			var result = await _mediaService.SetPrimaryMediaAsync(mediaId);
			return result;
		}

		public async Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId)
		{
			// Verify product exists
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Get media
			var result = await _mediaService.GetMediaByEntityAsync(EntityType.Product, productId);
			return result;
		}

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
            await _productRepo.AddProductEmbeddingsAsync(productId);
            return BaseResponse.Ok("Product embedding updated successfully");
        }
    }
}
