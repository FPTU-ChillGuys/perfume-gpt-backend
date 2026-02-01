using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly IProductRepository _productRepo;
		private readonly IMediaService _mediaService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly IMapper _mapper;

		public ProductService(
			IProductRepository productRepo,
			IMediaService mediaService,
			IUnitOfWork unitOfWork,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			IMapper mapper)
		{
			_productRepo = productRepo;
			_mediaService = mediaService;
			_unitOfWork = unitOfWork;
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

			// Convert temporary media to permanent media if provided
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, product.Id);
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
			var product = await _productRepo.GetProductWithDetailsAsync(productId);

			if (product == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<ProductResponse>(product);

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductsWithDetailsAsync(request);

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

			// === IMAGE MANAGEMENT ===

			// Delete specified images first
			if (request.MediaIdsToDelete != null && request.MediaIdsToDelete.Count != 0)
			{
				foreach (var mediaId in request.MediaIdsToDelete)
				{
					var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
					if (!deleteResult.Success)
					{
						Console.WriteLine($"Warning: Failed to delete media {mediaId}: {deleteResult.Message}");
					}
				}
			}

			// Add new images from temporary media
			if (request.TemporaryMediaIdsToAdd != null && request.TemporaryMediaIdsToAdd.Count != 0)
			{
				await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIdsToAdd, productId);
			}

			_mapper.Map(request, product);

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to update product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product updated successfully");
		}

		#region Media Management

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
			await _productRepo.AddProductEmbeddingsAsync(productId);
			return BaseResponse.Ok("Product embedding updated successfully");
		}

		#endregion

		#region Private Methods

		private async Task ConvertTemporaryMediaToPermanentAsync(List<Guid> temporaryMediaIds, Guid productId)
		{
			foreach (var tempMediaId in temporaryMediaIds)
			{
				// Get temporary media
				var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
				if (tempMedia == null || tempMedia.IsExpired)
				{
					continue; // Skip if not found or expired
				}

				// Create permanent media from temporary
				var media = new Media
				{
					Url = tempMedia.Url,
					AltText = tempMedia.AltText,
					EntityType = EntityType.Product,
					ProductId = productId,
					DisplayOrder = tempMedia.DisplayOrder,
					IsPrimary = tempMedia.IsPrimary,
					PublicId = tempMedia.PublicId,
					FileSize = tempMedia.FileSize,
					MimeType = tempMedia.MimeType
				};

				await _unitOfWork.Media.AddAsync(media);

				_unitOfWork.TemporaryMedia.Remove(tempMedia);
			}
			await _unitOfWork.SaveChangesAsync();
		}

		#endregion
	}
}
